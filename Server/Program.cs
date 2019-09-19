using System;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using Fleck;
using Mono.Unix;
using Mono.Unix.Native;

namespace CodeGoat.Server
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            // ports
            int httpPort = 8080;
            int webSocketPort = 8181;

            if (args.Length != 0 && args.Length != 2)
            {
                Console.WriteLine("Usage: Server.exe [http-port] [ws-port]");
                Console.WriteLine("Use 8080 for HTTP and 8181 for websockets");
                return;
            }

            if (args.Length == 2)
            {
                httpPort = int.Parse(args[0]);
                webSocketPort = int.Parse(args[1]);
            }

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
            
            string pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            Console.WriteLine($"Applcation starts with PID { pid }");
            Console.WriteLine($"Http server running at port { httpPort }");
            Console.WriteLine($"Web socket server running at port { webSocketPort }");
            Console.WriteLine("");

            Log.Info("Application is running.");

            // wait for termination
            if (IsRunningOnMono())
            {
                UnixSignal.WaitAny(GetUnixTerminationSignals());
            }
            else
            {
                Console.WriteLine("Press enter to stop the application.");
                Console.ReadLine();
            }

            Log.Info("Stopping...");

            httpServer.Stop();
            editorServer.Stop();
        }

        private static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private static UnixSignal[] GetUnixTerminationSignals()
        {
            return new[]
            {
                new UnixSignal(Signum.SIGINT),
                new UnixSignal(Signum.SIGTERM),
                new UnixSignal(Signum.SIGQUIT),
                new UnixSignal(Signum.SIGHUP)
            };
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
