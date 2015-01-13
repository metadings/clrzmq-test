﻿using System;
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
			CancellationTokenSource cancellor0 = null;
			var monitors = new List<Thread>();
			CancellationTokenSource cancellor1 = doMonitor ? new CancellationTokenSource() : null;

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

					var serverThread = new Thread(() => RouterDealer_Server(cancellor0.Token, j, arg, doMonitor));
					serverThread.Start();
					serverThread.Join(64);

					if (doMonitor) {
						Thread monitorThread = new Thread(() => {
							var monitor = ZMonitor.Create(context, "inproc://RouterDealer-Server" + j);
							monitor.AllEvents += (sender, e) => { Console.WriteLine("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event)); };
							monitor.Run(cancellor1.Token); 
						});
						monitors.Add(monitorThread);

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

					if (doMonitor)
					{
						Thread monitorThread = new Thread(() =>
						{
							var monitor = ZMonitor.Create(context, "inproc://RouterDealer-Client" + j);
							monitor.AllEvents += (sender, e) => { Console.WriteLine("  {0}: {1}", arg, Enum.GetName(typeof(ZMonitorEvents), e.Event.Event)); };
							monitor.Run(cancellor1.Token);
						});
						monitors.Add(monitorThread);

						Console.WriteLine(RouterDealer_Client(j, arg, () => { monitorThread.Start(); }));
					}
					else {
						Console.WriteLine(RouterDealer_Client(j, arg, null));
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
						// Cancel the Monitors
						cancellor1.Cancel();
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
				};

				Console.WriteLine("Running...");

				while (true)
				{
					Thread.Sleep(250);
				}
			}

			if (cancellor1 != null)
			{
				// Cancel the Monitors
				cancellor1.Cancel();
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
							Thread.Sleep(500);

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

		static string RouterDealer_Client(int j, string name, Action monitor)
		{
			string output = null;

			using (var socket = ZSocket.Create(context, ZSocketType.REQ))
			{
				if (monitor != null)
				{
					socket.Monitor("inproc://RouterDealer-Client" + j);
					monitor();
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