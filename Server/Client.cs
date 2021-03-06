﻿using System;
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
        /// Client's name
        /// </summary>
        private string name;
        public string Name
        {
            get => name;
            
            /// <summary>
            /// Call only from inside the Room when having a sync lock obtained
            /// to prevent reading Name by other thread while writing
            /// </summary>
            set
            {
                if (String.IsNullOrEmpty(value))
                    name = "anonymous";
                else
                    name = value;
            }
        }

        /// <summary>
        /// Client's color
        /// 
        /// Set only from inside the Room when having a sync lock obtained
        /// to prevent reading Color by other thread while writing
        /// </summary>
        public string Color { get; set; }

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
            Log.Info($"New client {Id} has connected.");
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

            Log.Info($"Client {Id} has disconnected.");
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

            Log.Info($"Client {Id} sent message {message} and it was not understood.");
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
