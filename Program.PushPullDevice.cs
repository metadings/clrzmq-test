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
		public static void PushPullDevice(IDictionary<string, string> dict, string[] args)
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

			// We may have a cancellor and a pullDealer
			CancellationTokenSource cancellor0 = null;
			PushPullDevice pullDealer = null;

			var cancellor1 = doMonitor ? new CancellationTokenSource() : null;

			if (who == 0 || who == 1)
			{
				// Create the "Server" cancellor
				cancellor0 = new CancellationTokenSource();

				// Setup the Dealer
				pullDealer = new PushPullDevice(context, Frontend, Backend);
				pullDealer.Start(cancellor0).Join(64);

				int i = -1;
				foreach (string name in args)
				{
					int j = ++i;

					// Create a Server
					var serverThread = new Thread(() => PushPullDevice_Server(cancellor0.Token, j, name, doMonitor));
					serverThread.Start();
					serverThread.Join(64);

					if (doMonitor) {
						var monitor = ZMonitor.Create(context, "inproc://PushPullDevice-Server" + j);
						
						monitor.AllEvents += (sender, e) =>
						{
							Console.Write("  {0}: {1}", name, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event));
							if (e.Event.EventValue > 0) Console.Write(" ({0})", e.Event.EventValue);
							Console.WriteLine();
						};

						monitor.Start(cancellor1).Join(64);
					}
				}
			}

			if (who == 1)
			{
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

					if (pullDealer != null)
					{
						// Cancel the Device
						pullDealer.Stop();
					}
				};

				Console.WriteLine("Running...");

				while (true)
				{
					Thread.Sleep(64);
				}
			}

			if (who == 0 || who == 2)
			{
				int i = -1;
				foreach (string arg in args)
				{
					int j = ++i;

					// foreach arg we are the Client, asking the Server

					if (doMonitor) {
						var monitor = ZMonitor.Create(context, "inproc://PushPullDevice-Client" + j);
						
						monitor.AllEvents += (sender, e) =>
						{
							Console.Write("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event));
							if (e.Event.EventValue > 0) Console.Write(" ({0})", e.Event.EventValue);
							Console.WriteLine();
						};

						PushPullDevice_Client(j, arg, () => { monitor.Start(cancellor1).Join(64); });
					} 
					else {
						PushPullDevice_Client(j, arg);
					}
				}

				Thread.Sleep(250);
			}

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

			if (pullDealer != null)
			{
				// Cancel the Device
				pullDealer.Stop();
			}

			// we could have done here context.Terminate()

		}

		static void PushPullDevice_Server(CancellationToken cancellus, int i, string name, bool doMonitor)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PULL))
			{
				if (doMonitor) socket.Monitor("inproc://PushPullDevice-Server" + i);

				socket.Connect(Backend);

				ZError error;
				ZMessage request = null;

				while (!cancellus.IsCancellationRequested)
				{
					if (!socket.ReceiveMessage(ZSocketFlags.DontWait, ref request, out error))
					{
						if (error == ZError.EAGAIN)
						{
							error = ZError.None;
							Thread.Sleep(1);

							continue;
						}

						throw new ZException(error);
					}

					foreach (ZFrame frame in request)
					{
						if (frame.Length == 0) continue;

						string strg = frame.ReadString();
						Console.WriteLine("{0} said hello to {1}!", strg, name);
					}

					request = null;
				}
			}
		}

		static void PushPullDevice_Client(int j, string name, Action monitor = null)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PUSH))
			{
				if (monitor != null) { socket.Monitor("inproc://PushPullDevice-Client" + j); monitor(); }

				socket.Connect(Frontend);

				using (var request = new ZMessage())
				{
					request.Add(new ZFrame(name));

					socket.SendMessage(request);
				}
			}
		}
	}
}