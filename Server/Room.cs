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

                client.Send(
                    new JsonObject()
                        .Add("type", "document-state")
                        .Add("document", document.GetText())
                        .Add("initial", true)
                );
            }
        }

        /// <summary>
        /// Called when a client leaves the room
        /// </summary>
        public void OnClientLeft(Client client)
        {
            Console.WriteLine($"Client {client.Id} left the room '{Id}'.");

            lock (syncLock)
                clients.Remove(client);
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

                default:
                    Console.WriteLine(
                        $"Client {client.Id} sent message {message} to the room '{Id}' and it wasn't understood."
                    );
                    break;
            }
        }

        /// <summary>
        /// Change message was received
        /// (Some client changed his document, now we need to broadcast the change to others)
        /// </summary>
        private void ChangeReceived(Client from, JsonObject change)
        {
            lock (syncLock)
            {
                // change document
                document.ApplyChange(
                    Change.FromJsonObject(change, document)
                );

                // broadcast change
                foreach (Client client in clients)
                {
                    client.Send(
                        new JsonObject()
                            .Add("type", "change-broadcast")
                            .Add("familiar", client == from)
                            .Add("change", change)
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
    }
}
