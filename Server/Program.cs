using System;

namespace CodeGoat.Server
{
	class MainClass
	{
		public static void Main (string[] args)
		{
            var jo = new LightJson.JsonObject().Add("foo", 42);
            Console.WriteLine(jo.ToString());
		}
	}
}
