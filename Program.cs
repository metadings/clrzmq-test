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
	}
}