using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using ZeroMQ;

namespace ZeroMQ.Test
{
	static partial class Program
	{
		public static void PubSub(IDictionary<string, string> dict, string[] args)
		{
			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}
			Console.WriteLine(Frontend);

			// Setup the ZContext
			context = ZContext.Create();

			var cancellor0 = new CancellationTokenSource();

			if (!dict.ContainsKey("--server") || dict["--server"] == "+")
			{
				var serverThread = new Thread(() => PubSub_Server(cancellor0.Token));
				serverThread.Start();
				serverThread.Join(64);
			}

			foreach (string arg in args)
			{
				var clientThread = new Thread(() => PubSub_Client(cancellor0.Token, arg));
				clientThread.Start();
				clientThread.Join(64);
			}

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				Console.WriteLine("Cancelled...");

				if (cancellor0 != null)
				{
					// Cancel the Server
					cancellor0.Cancel();
				}

				// we could have done here context.Terminate()
			};

			Console.WriteLine("Running...");

			while (true)
			{
				Thread.Sleep(64);
			}

		}

		static void PubSub_Server(CancellationToken cancellus)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PUB))
			{
				socket.Bind(Frontend);

				var now = DateTime.Now;

				while (!cancellus.IsCancellationRequested)
				{
					if (now.AddSeconds(5) >= DateTime.Now)
					{
						Thread.Sleep(1);
						continue;
					}
					now = DateTime.Now;

					using (var response = new ZMessage())
					{
						response.Add(ZFrame.Create(DateTime.Now.ToString("G")));

						socket.SendMessage(response);
					}
				}

				socket.Unbind(Frontend);
			}
		}

		static void PubSub_Client(CancellationToken cancellus, string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.SUB))
			{
				socket.Connect(Frontend);

				socket.SubscribeAll();

				var poller = ZPollItem.Create(socket, (ZSocket _socket, out ZMessage message, out ZError _error) =>
				{
					if (null == (message = _socket.ReceiveMessage(out _error)))
					{
						throw new ZException(_error);
					}
					return true;
				});

				while (!cancellus.IsCancellationRequested)
				{
					ZError error;
					ZMessage message;
					if (!poller.TryPollIn(out message, out error, TimeSpan.FromMilliseconds(512)))
					{
						if (error == ZError.EAGAIN)
						{
							error = ZError.None;
							Thread.Sleep(1);

							continue;
						}
						throw new ZException(error);
					}

					Console.WriteLine(name + " received " + message[0].ReadString());
				}

				socket.Disconnect(Frontend);
			}
		}
	}
}