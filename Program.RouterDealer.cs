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

		public static void RouterDealer(IDictionary<string, string> dict, string[] args)
		{
			int who = dict.ContainsKey("--server") ? 1 : (dict.ContainsKey("--client") ? 2 : 0);

			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}

			// Setup the ZContext
			context = ZContext.Create();

			RouterDealerDevice routerDealer = null;
			CancellationTokenSource cancellor0 = null;

			if (who == 0 || who == 1)
			{
				routerDealer = new RouterDealerDevice(context, Frontend, Backend);
				routerDealer.Start();
				routerDealer.Join(TimeSpan.FromMilliseconds(64));

				// Create the "Server" cancellor and thread
				cancellor0 = new CancellationTokenSource();

				for (int i = 0; i < 4; ++i)
				{
					var serverThread = new Thread(() => RouterDealer_Server(cancellor0.Token, args[0]));
					serverThread.Start();
					serverThread.Join(64);
				}
			}

			if (who == 1)
			{
				Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
				{
					Console.WriteLine("Cancelled...");
				};

				Console.WriteLine("Running...");

				while (true)
				{
					Thread.Sleep(64);
				}
			}
			else if (who == 0 || who == 2)
			{
				// foreach arg we are the Client, asking the Server
				foreach (string arg in args)
				{
					Console.WriteLine(RouterDealer_Client(arg));
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

		static void RouterDealer_Server(CancellationToken cancellus, string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.REP))
			{
				socket.Connect(Backend);

				ZError error;
				ZMessage request;

				while (!cancellus.IsCancellationRequested)
				{
					if (null == (request = socket.ReceiveMessage(ZSocketFlags.DontWait, out error)))
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
						response.Add(ZFrame.Create(name + " says hello to " + request[0].ReadString()));
						socket.SendMessage(response);
					}
				}
			}
		}

		static string RouterDealer_Client(string name)
		{
			string output = null;

			using (var socket = ZSocket.Create(context, ZSocketType.REQ))
			{
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