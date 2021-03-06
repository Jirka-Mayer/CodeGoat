﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// When was the last time a client has left the room
        /// </summary>
        private DateTime lastClientLeftAt = DateTime.Now; // screw null values

        /// <summary>
        /// After how many seconds can the room be removed for being abandoned?
        /// </summary>
        private const int DieAfterSeconds = 60 * 60; // 1 hour

        public Room(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Called when a client joins the room
        /// </summary>
        public void OnClientJoined(Client client)
        {
            Log.Info($"Client {client.Id} joined the room '{Id}'.");

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
                        .Add("document-state", document.State)
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

                lastClientLeftAt = DateTime.Now;

                Log.Info(
                    $"Client {client.Id} left the room '{Id}'. {clients.Count} clients remaining inside."
                );
            }
        }

        /// <summary>
        /// Returns true if this room has been abandoned for
        /// suficient amount of time and can be removed
        /// </summary>
        public bool IsDead()
        {
            lock (syncLock)
            {
                return clients.Count == 0 && (DateTime.Now - lastClientLeftAt).TotalSeconds > DieAfterSeconds;
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
                    ChangeReceived(
                        client,
                        message["document-state"].AsString,
                        message["dependencies"].AsJsonArray.Select(x => x.AsString).ToList(),
                        message["change"].AsJsonObject
                    );
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
                    Log.Info(
                        $"Client {client.Id} sent message {message} to the room '{Id}' and it wasn't understood."
                    );
                    break;
            }
        }

        /// <summary>
        /// Change message was received
        /// (Some client changed their document, now we need to broadcast the change to others)
        /// </summary>
        private void ChangeReceived(
            Client from,
            string documentState,
            List<string> dependencies,
            JsonObject jsonChange
        )
        {
            lock (syncLock)
            {
                // change instance has to be created inside the lock, because it accesses the document
                // do NOT clamp to the document dimensions here !!!
                Change change = Change.FromCodemirrorJson(jsonChange);

                // handle location update for changes that were overrun
                // by newer chagnes on their way to the server
                Change updatedChange = document.UpdateChangeLocationByNewerChanges(
                    change, documentState, dependencies
                );

                // the document state was not found in document history
                if (updatedChange == null)
                {
                    Log.Info("Received a change that was made in an unknown document state.");
                    BroadcastDocumentState();
                    return;
                }

                // change document
                document.ApplyChange(updatedChange);

                // broadcast change
                foreach (Client client in clients)
                {
                    client.Send(
                        new JsonObject()
                            .Add("type", "change-broadcast")
                            .Add("change", updatedChange.ToCodemirrorJson())
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
                            .Add("document-state", document.State)
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
