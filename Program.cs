using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ZeroMQ;

namespace ZeroMQ.Test
{
	static partial class Program
	{
		static ZContext context;

		public static string Frontend = "tcp://127.0.0.1:2772";
		public static string Backend = "tcp://127.0.0.1:2773";

		public static void Has(IDictionary<string, string> dict, string[] args)
		{
			if (args == null || args.Length == 0)
			{
				args = new string[] { "ipc", "pgm", "tipc", "norm", "curve", "gssapi" };
			}

			foreach (string arg in args)
				Console.WriteLine("{0}: {1}", arg, ZContext.Has(arg));
		}

		public static void Version(IDictionary<string, string> dict, string[] args)
		{
			Console.WriteLine(ZeroMQ.lib.zmq.Version);
		}
	}
}