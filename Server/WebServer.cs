using System;
using System.Net;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeGoat.Server
{
    public class WebServer
    {
        private HttpListener listener = new HttpListener();

        private List<Route> routes = new List<Route>();

        private class Route
        {
            public Regex pattern;
            public string contentType;
            public Func<HttpListenerRequest, Match, string> handler;
        }

        public WebServer(int port = 80)
        {
            listener.Prefixes.Add("http://*:" + port.ToString() + "/");
        }
 
        public void Run()
        {
            listener.Start();

            ThreadPool.QueueUserWorkItem((o) => {
                while (listener.IsListening)
                {
                    HttpListenerContext ctx;

                    try
                    {
                        ctx = listener.GetContext(); // block & wait for a request
                    }
                    catch (HttpListenerException) {
                        continue; // listener has been stopped, probbably
                    }

                    // process the request asynchronously
                    ThreadPool.QueueUserWorkItem((c) => {
                        var context = c as HttpListenerContext;
                        try
                        {
                            Tuple<string, string> response = HandleRequest(context.Request);
                            byte[] responseBytes = Encoding.UTF8.GetBytes(response.Item1);
                            context.Response.Headers.Add("Content-Type", response.Item2);
                            context.Response.ContentLength64 = responseBytes.Length;
                            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                        }
                        finally
                        {
                            context.Response.OutputStream.Close();
                        }
                    }, ctx);
                }
            });
        }
 
        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }

        private Tuple<string, string> HandleRequest(HttpListenerRequest request)
        {
            foreach (Route r in routes)
            {
                Match m = r.pattern.Match(request.Url.AbsolutePath);

                if (m.Success)
                {
                    return new Tuple<string, string>(
                        r.handler(request, m),
                        r.contentType
                    );
                }
            }

            return new Tuple<string, string>(
                "Nothing here. 404",
                "text/plain"
            );
        }

        public void On(string pattern, string contentType, Func<HttpListenerRequest, Match, string> handler)
        {
            routes.Add(new Route {
                pattern = new Regex("^" + pattern + "$"),
                contentType = contentType,
                handler = handler
            });
        }
    }
}
