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

		public static void RouterDealer(IDictionary<string, string> dict, string[] args)
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

			RouterDealerDevice routerDealer = null;
			var monitors = new List<ZMonitor>();
			CancellationTokenSource cancellor0 = null;

			if (who == 0 || who == 1)
			{
				routerDealer = new RouterDealerDevice(context, Frontend, Backend);
				routerDealer.Start();
				routerDealer.Join(TimeSpan.FromMilliseconds(64));

				// Create the "Server" cancellor and thread
				cancellor0 = new CancellationTokenSource();

				int i = -1;
				foreach (string arg in args)
				{
					int j = ++i;
					Thread monitorThread = null;

					if (doMonitor) {
						var monitor = ZMonitor.Create(context, "inproc://RouterDealer-Server" + j);
						monitorThread = new Thread(() => { 
							monitor.AllEvents += (sender, e) => { Console.WriteLine("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event)); };
							monitor.Run(cancellor0.Token); 
						});
						monitors.Add(monitor);
					}

					var serverThread = new Thread(() => RouterDealer_Server(cancellor0.Token, j, arg, doMonitor));
					serverThread.Start();
					serverThread.Join(64);

					if (doMonitor)
					{
						monitorThread.Start();
						monitorThread.Join(64);
					}
				}
			}
			
			if (who == 0 || who == 2)
			{

				// foreach arg we are the Client, asking the Server
				int i = -1;
				foreach (string arg in args)
				{
					int j = ++i;
					Thread monitorThread = null;

					if (doMonitor)
					{
						var monitor = ZMonitor.Create(context, "inproc://RouterDealer-Client" + j);
						monitorThread = new Thread(() =>
						{
							monitor.AllEvents += (sender, e) => { Console.WriteLine("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event)); };
							monitor.Run(cancellor0.Token);
						});
						monitors.Add(monitor);
					}

					Console.WriteLine(RouterDealer_Client(j, arg, doMonitor));

					if (doMonitor)
					{
						monitorThread.Start();
					}
				}
			}

			if (who == 1)
			{
				Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
				{
					Console.WriteLine("Cancelled...");

					if (cancellor0 != null)
					{
						// Cancel the Server
						cancellor0.Cancel();
					}

					if (routerDealer != null)
					{
						routerDealer.Stop();
					}
				};

				Console.WriteLine("Running...");

				while (true)
				{
					Thread.Sleep(250);
				}
			}

			if (cancellor0 != null)
			{
				// Cancel the Server
				cancellor0.Cancel();
			}

			if (routerDealer != null)
			{
				routerDealer.Stop();
			}

			// we could have done here context.Terminate()

		}

		static void RouterDealer_Server(CancellationToken cancellus, int i, string name, bool doMonitor)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.REP))
			{
				if (doMonitor)
				{
					socket.Monitor("inproc://RouterDealer-Server" + i);
				}

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
							Thread.Sleep(1000);

							continue;
						}

						throw new ZException(error);
					}

					// Let the response be "Hello " + input
					using (request)
					using (var response = new ZMessage())
					{
						response.Add(ZFrame.Create(name + " says hello to " + request[0].ReadString()));
						socket.SendMessage(response);
					}

					request = null;
				}
			}
		}

		static string RouterDealer_Client(int j, string name, bool doMonitor)
		{
			string output = null;

			using (var socket = ZSocket.Create(context, ZSocketType.REQ))
			{
				if (doMonitor)
				{
					socket.Monitor("inproc://RouterDealer-Client" + j);
				}

				socket.Connect(Frontend);

				using (var request = new ZMessage())
				{
					request.Add(ZFrame.Create(name));
					socket.SendMessage(request);
				}

				using (ZMessage response = socket.ReceiveMessage())
				{
					output = response[0].ReadString();
				}
			}

			return output;
		}
	}
}