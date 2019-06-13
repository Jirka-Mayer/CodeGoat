using System;
using LightJson;

namespace CodeGoat.Server
{
    /// <summary>
    /// A single file that people colaborate on
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Room identifier
        /// </summary>
        public string Id { get; private set; }

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
        }

        /// <summary>
        /// Called when a client leaves the room
        /// </summary>
        public void OnClientLeft(Client client)
        {
            Console.WriteLine($"Client {client.Id} left the room '{Id}'.");
        }

        /// <summary>
        /// Called when a client sends a message to the server and the message
        /// is targeted at the room
        /// </summary>
        public void OnClientSentMessage(Client client, JsonObject message)
        {
            Console.WriteLine($"Client {client.Id} sent message to the room '{Id}': {message}.");
        }
    }
}
