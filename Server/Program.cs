using System;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace CodeGoat.Server
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            int httpPort = 8080;

			WebServer ws = new WebServer(httpPort);
            
            RegisterWebServerRoutes(ws);
			ws.Run();
            
            Console.WriteLine($"Web server running at port { httpPort }...");
			Console.WriteLine("Press a key to quit.");
			Console.ReadKey();

            ws.Stop();
		}

        private static void RegisterWebServerRoutes(WebServer ws)
        {
            ws.On("/", "text/plain", (request, match) => {
                return "Hello!";
            });

            ws.On("/room/(.+)", "text/html", (request, match) => {
                return File.ReadAllText("html/room.html")
                    .Replace("%RoomId%", match.Groups[1].Value);
            });

            ws.On("/js/(.+)", "application/javascript", (request, match) => {
                return File.ReadAllText("." + match.Groups[0].Value);
            });

            ws.On("/css/(.+)", "text/css", (request, match) => {
                return File.ReadAllText("." + match.Groups[0].Value);
            });
        }
	}
}
