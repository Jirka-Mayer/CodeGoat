using System;

namespace CodeGoat.Server
{
    /// <summary>
    /// Formats console logging
    /// </summary>
    public static class Log
    {
        public static void Info(string message)
        {
            Console.WriteLine(
                $"[{DateTime.Now.ToString("yyyy-dd-MM H:mm:ss")}] {message}"
            );
        }
    }
}
