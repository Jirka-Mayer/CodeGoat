using System;
using System.Collections.Generic;
using LightJson;

namespace CodeGoat.Server
{
    /// <summary>
    /// A single file that people collaborate on
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Room identifier
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Lock for synchronizing critical actions
        /// - document editing
        /// - client addition / removal
        /// </summary>
        private object syncLock = new object();

        /// <summary>
        /// The underlying text document
        /// </summary>
        private Document document = new Document();

        /// <summary>
        /// Clients connected to the room
        /// </summary>
        private List<Client> clients = new List<Client>();

        /// <summary>
        /// Helper for generating unique colors for clients
        /// </summary>
        private ColorGenerator colorGenerator = new ColorGenerator();

        public Room(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Called when a client joins the room
        /// </summary>
        public void OnClientJoined(Client client)
        {
            Console.WriteLine($"Client {client.Id} joined the room '{Id}'.");

            lock (syncLock)
            {
                if (clients.Contains(client))
                    throw new ArgumentException("The client has already joined the room.");

                clients.Add(client);

                client.Color = colorGenerator.NextColor();

                client.Send(
                    new JsonObject()
                        .Add("type", "document-state")
                        .Add("document", document.GetText())
                        .Add("initial", true)
                );

                BroadcastClientUpdate(client, justJoined: true);
            }
        }

        /// <summary>
        /// Called when a client leaves the room
        /// </summary>
        public void OnClientLeft(Client client)
        {
            Console.WriteLine($"Client {client.Id} left the room '{Id}'.");

            lock (syncLock)
            {
                clients.Remove(client);

                colorGenerator.ReleaseColor(client.Color);

                foreach (Client c in clients)
                {
                    c.Send(
                        new JsonObject()
                            .Add("type", "client-left")
                            .Add("clientId", client.Id)
                    );
                }
            }
        }

        /// <summary>
        /// Called when a client sends a message to the server and the message
        /// is targeted at the room
        /// </summary>
        public void OnClientSentMessage(Client client, JsonObject message)
        {
            switch (message["type"].AsString)
            {
                case "change":
                    ChangeReceived(client, message["change"].AsJsonObject);
                    break;

                case "selection-change":
                    SelectionChangeReceived(client, message["selection"].AsJsonObject);
                    break;

                case "request-document-broadcast":
                    BroadcastDocumentState();
                    break;

                case "name-changed":
                    lock (syncLock)
                        client.Name = message["name"].AsString;

                    BroadcastClientUpdate(client, justJoined: false);
                    break;

                default:
                    Console.WriteLine(
                        $"Client {client.Id} sent message {message} to the room '{Id}' and it wasn't understood."
                    );
                    break;
            }
        }

        /// <summary>
        /// Change message was received
        /// (Some client changed their document, now we need to broadcast the change to others)
        /// </summary>
        private void ChangeReceived(Client from, JsonObject jsonChange)
        {
            lock (syncLock)
            {
                // change instance has to be created inside the lock, because it accesses the document
                Change change = Change.FromCodemirrorJson(jsonChange, document);

                // TODO: handle coordinate update for old changes

                // change document
                document.ApplyChange(change);

                // broadcast change
                foreach (Client client in clients)
                {
                    client.Send(
                        new JsonObject()
                            .Add("type", "change-broadcast")
                            .Add("familiar", client == from)
                            .Add("change", change.ToCodemirrorJson())
                    );
                }
            }
        }

        /// <summary>
        /// Selection change message received
        /// Needs to be broadcasted to others
        /// </summary>
        private void SelectionChangeReceived(Client from, JsonObject selection)
        {
            lock (syncLock)
            {
                foreach (Client client in clients)
                {
                    if (client == from)
                        continue;

                    client.Send(
                        new JsonObject()
                            .Add("type", "selection-broadcast")
                            .Add("clientId", from.Id)
                            .Add("selection", selection)
                    );
                }
            }
        }

        /// <summary>
        /// Sends current document state to all clients
        /// </summary>
        public void BroadcastDocumentState()
        {
            lock (syncLock)
            {
                foreach (Client client in clients)
                {
                    client.Send(
                        new JsonObject()
                            .Add("type", "document-state")
                            .Add("document", document.GetText())
                            .Add("initial", false)
                    );
                }
            }
        }

        /// <summary>
        /// Broadcast update of a given client to all others (but not him)
        /// </summary>
        private void BroadcastClientUpdate(Client client, bool justJoined)
        {
            lock (syncLock)
            {
                // send others who this new client is
                foreach (Client c in clients)
                {
                    if (c == client)
                        continue;

                    c.Send(
                        new JsonObject()
                            .Add("type", "client-update")
                            .Add("clientId", client.Id)
                            .Add("name", client.Name)
                            .Add("color", client.Color)
                    );
                }

                // send to the new client, what other people exist in this room
                if (justJoined)
                {
                    foreach (Client c in clients)
                    {
                        if (c == client)
                            continue;

                        client.Send(
                            new JsonObject()
                                .Add("type", "client-update")
                                .Add("clientId", c.Id)
                                .Add("name", c.Name)
                                .Add("color", c.Color)
                        );
                    }
                }
            }
        }
    }
}
