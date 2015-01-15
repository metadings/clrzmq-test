using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ZeroMQ;
using ZeroMQ.Devices;
using ZeroMQ.Monitoring;

namespace ZeroMQ.Test
{
	static partial class Program
	{

		public static void StreamDealer(IDictionary<string, string> dict, string[] args)
		{
			int who = dict.ContainsKey("--server") ? 1 : (dict.ContainsKey("--client") ? 2 : 0);
			bool doMonitor = dict.ContainsKey("--monitor");

			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}

			// Setup the ZContext
			context = ZContext.Create();

			var cancellor0 = new CancellationTokenSource();

			var streamDealer = new StreamDealerDevice(context, Frontend, Backend);
			streamDealer.Start(cancellor0).Join(64);

			var cancellor1 = doMonitor ? new CancellationTokenSource() : null;

			int i = -1;
			foreach (string arg in args)
			{
				int j = ++i;

				var serverThread = new Thread(() => StreamDealer_Server(cancellor0.Token, j, arg, doMonitor));
				serverThread.Start();
				serverThread.Join(64);

				if (doMonitor)
				{
					var monitor = ZMonitor.Create(context, "inproc://StreamDealer-Server" + j);
					monitor.AllEvents += (sender, e) => { 
						Console.Write("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event));
						if (e.Event.EventValue > 0) Console.Write(" ({0})", e.Event.EventValue);
						if (!string.IsNullOrEmpty(e.Event.Address)) Console.Write(" ({0})", e.Event.Address);
						Console.WriteLine();
					};

					monitor.Start(cancellor1).Join(64);
				}
			}

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				Console.WriteLine("Cancelled...");

				if (cancellor1 != null)
				{
					// Cancel the Monitor
					cancellor1.Cancel();
				}

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

		static void StreamDealer_Server(CancellationToken cancellus, int i, string name, bool doMonitor)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.REP))
			{
				if (doMonitor) socket.Monitor("inproc://StreamDealer-Server" + i);

				socket.Connect(Backend);

				ZError error;
				ZMessage request;
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

					using (request)
					using (var response = new ZMessage())
					{
						response.Add(ZFrame.Create(
@"HTTP/1.1 200 OK
Content-Type: text/html; charset=UTF-8

<html>
<head>
	<title>Hello World!</title>
</head>
<body>
	<h3>Hello, I am " + name + @"!</h3>

	<div>Your Request:</div>
	<pre>" + request[0].ReadString() + @"</pre>

	<button id=""btnStop"">Stop</button>
	<script type=""text/javascript"">
		(function () {

			var timeout = setTimeout(function () { location.reload(true); }, 1000);

			addEventListener(""click"", function () {
				clearTimeout(timeout);
				var btn = document.getElementById(""btnStop"");
				btn.parentNode.removeChild(btn);
			});

		}());
	</script>

</body>
</html>"));
						socket.SendMessage(response);
					}
				}
			}
		}

	}
}