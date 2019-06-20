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

            // setup fleck logging
            FleckLog.LogAction = (level, message, ex) => {
                switch(level) {
                    case LogLevel.Error:
                    case LogLevel.Warn:
                        Console.WriteLine(message);
                        Console.WriteLine(ex);
                        break;
                    default:
                        // debug & info are not interesting
                        break;
                }
            };

            var editorServer = new EditorServer();
            var httpServer = new HttpServer(httpPort);
            var webSocketServer = new WebSocketServer("ws://0.0.0.0:" + webSocketPort);
            
            RegisterHttpServerRoutes(httpServer, webSocketPort);

			httpServer.Run();
            editorServer.Start();
            webSocketServer.Start(editorServer.HandleNewConnection);
            
            Console.WriteLine($"Http server running at port { httpPort }...");
            Console.WriteLine($"Web socket server running at port { webSocketPort }...");
			Console.WriteLine("Type 'exit' to end the application.");
            Console.WriteLine("");
			
            while (true)
            {
                string command = Console.ReadLine();

                if (command == "exit")
                {
                    Console.WriteLine("Stopping...");
                    break;
                }
            }

            httpServer.Stop();
            editorServer.Stop();
		}

        private static void RegisterHttpServerRoutes(HttpServer httpServer, int webSocketPort)
        {
            httpServer.On("/", "text/html", (request, match) => {
                return File.ReadAllText("html/index.html");
            });

            httpServer.On("/room/(.+)", "text/html", (request, match) => {
                return File.ReadAllText("html/room.html")
                    .Replace("%RoomId%", match.Groups[1].Value)
                    .Replace("%WebSocketPort%", webSocketPort.ToString());
            });

            httpServer.On("/js/(.+)", "application/javascript", (request, match) => {
                return File.ReadAllText("." + match.Groups[0].Value);
            });

            httpServer.On("/css/(.+)", "text/css", (request, match) => {
                return File.ReadAllText("." + match.Groups[0].Value);
            });
        }
	}
}
