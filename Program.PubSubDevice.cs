using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using ZeroMQ;
using ZeroMQ.Devices;
using ZeroMQ.Monitoring;

namespace ZeroMQ.Test
{
	static partial class Program
	{
		public static void PubSubDevice(IDictionary<string, string> dict, string[] args)
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
			PubSubDevice serverDevice = null;

			var cancellor1 = doMonitor ? new CancellationTokenSource() : null;

			if (who == 0 || who == 1)
			{
				if (who == 0 || dict["--server"] == "++")
				{
					serverDevice = new PubSubDevice(context, Frontend, Backend);
					serverDevice.Start(cancellor0).Join(64);
				}

				int i = -1;
				foreach (string arg in args)
				{
					int j = ++i;

					var serverThread = new Thread(() => PubSubDevice_Server(cancellor0.Token, j, arg, doMonitor));
					serverThread.Start();
					serverThread.Join(64);

					if (doMonitor) 
					{
						var monitor = ZMonitor.Create(context, "inproc://PubSubDevice-Server" + j);
						
						monitor.AllEvents += (sender, e) =>
						{
							Console.Write("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event));
							if (e.Event.EventValue > 0) Console.Write(" ({0})", e.Event.EventValue);
							Console.WriteLine();
						};

						monitor.Start(cancellor1).Join(64);
					}
				}
			}

			if (who == 0 || who == 2)
			{
				int i = -1;
				foreach (string arg in args)
				{
					int j = ++i;

					var clientThread = new Thread(() => PubSubDevice_Client(cancellor0.Token, j, arg, doMonitor));
					clientThread.Start();
					clientThread.Join(64);

					if (doMonitor)
					{
						var monitor = ZMonitor.Create(context, "inproc://PubSubDevice-Client" + j);
						
						monitor.AllEvents += (sender, e) =>
						{
							Console.Write("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event));
							if (e.Event.EventValue > 0) Console.Write(" ({0})", e.Event.EventValue);
							Console.WriteLine();
						};

						monitor.Start(cancellor1).Join(64);
					}
				}
			}

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				Console.WriteLine("Cancelled...");

				if (cancellor1 != null)
				{
					// Cancel the Server
					cancellor1.Cancel();
				}

				if (cancellor0 != null)
				{
					// Cancel the Server
					cancellor0.Cancel();
				}

				if (serverDevice != null)
				{
					serverDevice.Stop();
				}

				// we could have done here context.Terminate()
			};

			Console.WriteLine("Running...");

			while (true)
			{
				Thread.Sleep(250);
			}

		}

		static void PubSubDevice_Server(CancellationToken cancellus, int i, string name, bool doMonitor)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PUB))
			{
				if (doMonitor) socket.Monitor("inproc://PubSubDevice-Server" + i);

				socket.Connect(Frontend);

				var now = DateTime.Now;

				while (!cancellus.IsCancellationRequested)
				{
					if (now.AddSeconds(5) >= DateTime.Now)
					{
						Thread.Sleep(100);
						continue;
					}
					now = DateTime.Now;

					using (var response = new ZMessage())
					{
						response.Add(new ZFrame(
							string.Format("{0} {1}", DateTime.Now.ToString("G"), name)
						));

						socket.Send(response);
					}
				}

				socket.Disconnect(Frontend);
			}
		}

		static void PubSubDevice_Client(CancellationToken cancellus, int j, string name, bool doMonitor)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.SUB))
			{
				if (doMonitor) socket.Monitor("inproc://PubSubDevice-Client" + j);

				socket.Connect(Backend);

				socket.SubscribeAll();

				ZError error;
				ZMessage message;
				// var poller = ZPollItem.CreateReceiver(socket);

				while (!cancellus.IsCancellationRequested)
				{
					// if (!poller.PollIn(out message, out error, TimeSpan.FromMilliseconds(1000)))
					if (null == (message = socket.ReceiveMessage(ZSocketFlags.DontWait, out error)))
					{
						if (error == ZError.EAGAIN)
						{
							error = ZError.None;
							Thread.Sleep(1);

							continue;
						}
						throw new ZException(error);
					}

					using (message)
					{
						Console.WriteLine(
							string.Format("{0} received {1}", name, message[0].ReadString())
						);
					}
				}

				socket.Disconnect(Backend);
			}
		}
	}
}