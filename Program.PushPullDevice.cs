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
		public static void PushPullDevice(IDictionary<string, string> dict, string[] args)
		{
			int who = dict.ContainsKey("--server") ? 1 : (dict.ContainsKey("--client") ? 2 : 0);

			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}

			// Setup the ZContext
			context = ZContext.Create();

			PushPullDevice pullDealer = null;
			CancellationTokenSource cancellor0 = null;

			if (who == 0 || who == 1)
			{
				// Setup the Dealer
				pullDealer = new PushPullDevice(context, Frontend, Backend);
				pullDealer.Start();

				// Create the "Server" cancellor and threads
				cancellor0 = new CancellationTokenSource();

				for (int i = 0; i < 4; ++i)
				{
					var serverThread = new Thread(PushPullDevice_Server);
					serverThread.Start(cancellor0.Token);
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
					PushPullDevice_Client(arg);
				}

				Thread.Sleep(1000);
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

		static void PushPullDevice_Server(object cancelluS)
		{
			var cancellus = (CancellationToken)cancelluS;

			using (var socket = ZSocket.Create(context, ZSocketType.PULL))
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

					foreach (ZFrame frame in request)
					{
						if (frame.Length == 0) continue;

						string strg = frame.ReadString();
						Console.WriteLine("{0} said hello!", strg);
					}
				}
			}
		}

		static void PushPullDevice_Client(string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PUSH))
			{
				socket.Connect(Frontend);

				using (var request = new ZMessage())
				{
					request.Add(ZFrame.Create(name));

					socket.SendMessage(request);
				}
			}
		}
	}
}