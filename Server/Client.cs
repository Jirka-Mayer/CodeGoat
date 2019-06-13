using System;
using System.Collections;
using System.Collections.Generic;
using Fleck;
using LightJson;

namespace CodeGoat.Server
{
    /// <summary>
    /// Represents a single connected client
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Underlying connection
        /// </summary>
        private IWebSocketConnection connection;

        /// <summary>
        /// Client identifier
        /// </summary>
        public int Id => connection.ConnectionInfo.ClientPort;

        /// <summary>
        /// Room this client belongs to
        /// 
        /// Accessed from multiple threads so it needs to be locked (by the roomLock)
        /// </summary>
        private Room room;
        private object roomLock = new object();

        /// <summary>
        /// Returns a room instance given a string identifier
        /// </summary>
        private Func<string, Room> roomResolver;

        public Client(IWebSocketConnection connection, Func<string, Room> roomResolver)
        {
            // NOTE: do not register connection events, they are register by the EditorServer
            this.connection = connection;

            this.roomResolver = roomResolver;
        }

        /// <summary>
        /// Called when the connection is established
        /// 
        /// Runs within some Fleck thread
        /// </summary>
        public void OnConnect()
        {
            Console.WriteLine($"New client {Id} has connected.");
        }

        /// <summary>
        /// Called when the connection is closed
        /// 
        /// Runs within some Fleck thread
        /// </summary>
        public void OnDisconnect()
        {
            lock (roomLock)
            {
                if (room != null)
                {
                    room.OnClientLeft(this);
                    room = null;
                }
            }

            Console.WriteLine($"Client {Id} has disconnected.");
        }

        /// <summary>
        /// Called when a message arrives
        /// 
        /// Runs within some Fleck thread
        /// </summary>
        public void OnMessage(JsonObject message)
        {
            lock (roomLock)
            {
                if (room == null && message["type"] == "join-room")
                {
                    room = roomResolver(message["room"]);
                    room.OnClientJoined(this);
                    return;
                }

                if (room != null)
                {
                    room.OnClientSentMessage(this, message);
                    return;
                }
            }

            Console.WriteLine($"Client {Id} sent message {message} and it was not understood.");
        }

        /// <summary>
        /// Sends a message to the client
        /// 
        /// Callable from any thread
        /// </summary>
        public void Send(JsonObject message)
        {
            connection.Send(message.ToString());
        }
    }
}
