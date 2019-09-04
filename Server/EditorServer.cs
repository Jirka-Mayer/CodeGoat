using System;
using System.Collections;
using System.Collections.Generic;
using Fleck;
using LightJson;
using LightJson.Serialization;

namespace CodeGoat.Server
{
    /// <summary>
    /// Handles all the editor logic via web-socket connections
    /// </summary>
    public class EditorServer
    {
        /// <summary>
        /// List of connected clients
        /// 
        /// Edited only on client connection and disconnection
        /// Accessed in parallel, so needs to be locked while accessed
        /// </summary>
        private List<Client> clients = new List<Client>();

        /// <summary>
        /// Set of alive rooms, key is the room identifier
        /// 
        /// Accessed in parallel, so it needs to be locked
        /// </summary>
        private Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        /// <summary>
        /// Triggers document state broadcasting
        /// </summary>
        private DocumentBroadcaster documentBroadcaster;

        public EditorServer()
        {
            documentBroadcaster = new DocumentBroadcaster(BroadcastDocumentStates);
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            documentBroadcaster.Start();
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void Stop()
        {
            documentBroadcaster.Stop();
        }

        /// <summary>
        /// Handles a new incomming web socket connection
        /// 
        /// Runs inside some Fleck thread
        /// </summary>
        public void HandleNewConnection(IWebSocketConnection connection)
        {
            var client = new Client(connection, ResolveRoom);

            // client connection
            lock (clients)
                clients.Add(client);

            connection.OnClose = () => {
                client.OnDisconnect();
                
                // client disconnection
                lock (clients)
                    clients.Remove(client);
            };

            connection.OnOpen = () => client.OnConnect();

            connection.OnMessage = (message) => client.OnMessage(JsonReader.Parse(message).AsJsonObject);
        }

        /// <summary>
        /// Returns (or creates) a room with the given identifier
        /// 
        /// Can be called from any thread
        /// </summary>
        public Room ResolveRoom(string identifier)
        {
            lock (rooms)
            {
                Room room;

                if (rooms.TryGetValue(identifier, out room))
                    return room;

                Log.Info("Creating a new room: " + identifier);

                room = new Room(identifier);
                rooms.Add(identifier, room);
                return room;
            }
        }

        /// <summary>
        /// Sends current document state to all clients in all rooms,
        /// called periodically
        /// </summary>
        private void BroadcastDocumentStates()
        {
            //Console.WriteLine("Broadcasting document state.");

            lock (rooms)
            {
                List<Room> roomsToRemove = new List<Room>();

                foreach (Room room in rooms.Values)
                {
                    room.BroadcastDocumentState();

                    // also when in business, check for any dead rooms
                    if (room.IsDead())
                        roomsToRemove.Add(room);
                }

                // remove dead rooms
                foreach (Room room in roomsToRemove)
                {
                    rooms.Remove(room.Id);
                    Log.Info("Removed a dead room: " + room.Id);
                }
            }
        }
    }
}
