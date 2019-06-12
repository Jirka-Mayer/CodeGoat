using System;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using Fleck;

namespace CodeGoat.Server
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            // ports
            int httpPort = 8080;
            int webSocketPort = 8181;

            // disable fleck logging
            FleckLog.LogAction = (level, message, ex) => {
                // nothing
            };

            var httpServer = new HttpServer(httpPort);
            var webSocketServer = new WebSocketServer("ws://0.0.0.0:" + webSocketPort);
            
            RegisterHttpServerRoutes(httpServer);

			httpServer.Run();
            webSocketServer.Start(HandleWebSocketServerConnection);
            
            Console.WriteLine($"Http server running at port { httpPort }...");
            Console.WriteLine($"Web socket server running at port { webSocketPort }...");
			Console.WriteLine("Press a key to quit.");
			Console.ReadKey();

            httpServer.Stop();
		}

        private static void RegisterHttpServerRoutes(HttpServer httpServer)
        {
            httpServer.On("/", "text/plain", (request, match) => {
                return "Hello!";
            });

            httpServer.On("/room/(.+)", "text/html", (request, match) => {
                return File.ReadAllText("html/room.html")
                    .Replace("%RoomId%", match.Groups[1].Value);
            });

            httpServer.On("/js/(.+)", "application/javascript", (request, match) => {
                return File.ReadAllText("." + match.Groups[0].Value);
            });

            httpServer.On("/css/(.+)", "text/css", (request, match) => {
                return File.ReadAllText("." + match.Groups[0].Value);
            });
        }

        private static void HandleWebSocketServerConnection(IWebSocketConnection connection)
        {
            connection.OnOpen = () => Console.WriteLine("Open!");
            connection.OnClose = () => Console.WriteLine("Close!");
            connection.OnMessage = message => connection.Send("Server echoes: " + message);
        }
	}
}
