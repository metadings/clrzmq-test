using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ZeroMQ;
using ZeroMQ.Devices;

namespace ZeroMQ.Test
{
	static partial class Program
	{

		public static void StreamDealer(IDictionary<string, string> dict, string[] args)
		{
			int who = dict.ContainsKey("--server") ? 1 : (dict.ContainsKey("--client") ? 2 : 0);

			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}

			// Setup the ZContext
			context = ZContext.Create();

			// Create the "Server" cancellor and thread
			var cancellor0 = new CancellationTokenSource();
			var streamDealer = new StreamDealerDevice(context, Frontend, Backend);
			streamDealer.Start();
			streamDealer.Join(TimeSpan.FromMilliseconds(64));

			foreach (string arg in args)
			{
				var serverThread = new Thread(() => StreamDealer_Server(cancellor0.Token, arg));
				serverThread.Start();
				serverThread.Join(64);
			}

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				Console.WriteLine("Cancelled...");

				if (cancellor0 != null)
				{
					// Cancel the Server
					cancellor0.Cancel();
				}

				if (streamDealer != null)
				{
					streamDealer.Stop();
				}

				// we could have done here context.Terminate()
			};

			Console.WriteLine("Running...");
			Console.WriteLine("Please start your browser on {0} ...", Frontend);

			while (true)
			{
				Thread.Sleep(250);
			}

		}

		static void StreamDealer_Server(CancellationToken cancellus, string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.REP))
			{
				socket.Connect(Backend);

				ZError error;
				ZMessage request = null;
				var poller = ZPollItem.CreateReceiver(socket);

				while (!cancellus.IsCancellationRequested)
				{
					if (!poller.PollIn(out request, out error, TimeSpan.FromMilliseconds(250)))
					{
						if (error == ZError.EAGAIN)
						{
							error = ZError.None;
							Thread.Sleep(1);

							continue;
						}

						throw new ZException(error);
					}

					// Let the response be "Hello " + input
					using (request)
					using (var response = new ZMessage())
					{
						response.Add(ZFrame.Create(
@"HTTP/1.1 200 OK
Content-Type: text/html

<html>
<head>
	<title>Hello World!</title>
</head>
<body>
	<h3>Hello, I am " + name + @"!</h3>
	<div>Your Request:</div>
	<pre>" + request[0].ReadString() + @"</pre>
</body>
</html>"));
						socket.SendMessage(response);
					}

					request = null;
				}
			}
		}

	}
}