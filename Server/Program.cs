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
			WebServer ws = new WebServer();

            ws.On("/", "text/plain", (request, match) => {
                return "Hello!";
            });

            ws.On("/room/(.+)", "text/plain", (request, match) => {
                return "This is the room " + match.Groups[1];
            });

			ws.Run();
            
            Console.WriteLine("Server running...");
			Console.WriteLine("Press a key to quit.");
			Console.ReadKey();
			
            ws.Stop();
		}
	}
}
