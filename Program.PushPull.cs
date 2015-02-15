using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ZeroMQ;

namespace Examples
{
	static partial class Program
	{
		public static void PushPull(IDictionary<string, string> dict, string[] args)
		{
			int who = dict.ContainsKey("--server") ? 1 : (dict.ContainsKey("--client") ? 2 : 0);

			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}

			// Setup the ZContext
			context = ZContext.Create();

			CancellationTokenSource cancellor0 = null;

			if (who == 0 || who == 1)
			{
				// Create the "Server" cancellor and thread
				cancellor0 = new CancellationTokenSource();
				var serverThread = new Thread(PushPull_Server);
				serverThread.Start(cancellor0.Token);
				serverThread.Join(64);
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
					PushPull_Client(arg);
				}

				Thread.Sleep(1000);
			}

			if (cancellor0 != null)
			{
				// Cancel the Server
				cancellor0.Cancel();
			}

			// we could have done here context.Terminate()

		}

		static void PushPull_Server(object cancelluS)
		{
			var cancellus = (CancellationToken)cancelluS;

			using (var socket = ZSocket.Create(context, ZSocketType.PULL))
			{
				socket.Bind(Frontend);

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
						Console.WriteLine(strg + " said hello!");
					}

					request = null;
				}
			}
		}

		static void PushPull_Client(string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PUSH))
			{
				socket.Connect(Frontend);

				using (var request = new ZMessage())
				{
					request.Add(new ZFrame(name));

					socket.Send(request);
				}
			}
		}
	}
}