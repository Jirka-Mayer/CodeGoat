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
            Console.WriteLine($"Client {client.Id} sent message to the room '{Id}': {message}.");

            // edit document
            // broadcast operation to all clients
        }
    }
}
