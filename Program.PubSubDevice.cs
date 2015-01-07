using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using ZeroMQ;
using ZeroMQ.Devices;

namespace ZeroMQ.Test
{
	static partial class Program
	{
		public static void PubSubDevice(IDictionary<string, string> dict, string[] args)
		{
			int who = dict.ContainsKey("--server") ? 1 : (dict.ContainsKey("--client") ? 2 : 0);

			if (args == null || args.Length < 1)
			{
				// say here were some arguments...
				args = new string[] { "World" };
			}

			// Setup the ZContext
			context = ZContext.Create();

			var cancellor0 = new CancellationTokenSource();
			PubSubDevice serverDevice = null;

			if (who == 0 || who == 1)
			{
				if (who == 0 || dict["--server"] == "++")
				{
					serverDevice = new PubSubDevice(context, Frontend, Backend);
					serverDevice.Start();
					// serverDevice.Join();
				}

				foreach (string arg in args)
				{
					var serverThread = new Thread(() => PubSubDevice_Server(cancellor0.Token, arg));
					serverThread.Start();
					serverThread.Join(64);
				}
			}

			if (who == 0 || who == 2)
			{
				foreach (string arg in args)
				{
					var clientThread = new Thread(() => PubSubDevice_Client(cancellor0.Token, arg));
					clientThread.Start();
					clientThread.Join(64);
				}
			}

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
			{
				Console.WriteLine("Cancelled...");

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
				Thread.Sleep(64);
			}

		}

		static void PubSubDevice_Server(CancellationToken cancellus, string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.PUB))
			{
				socket.Connect(Frontend);

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
						response.Add(ZFrame.Create(
							string.Format("{0} {1}", DateTime.Now.ToString("G"), name)
						));

						socket.SendMessage(response);
					}
				}

				socket.Disconnect(Frontend);
			}
		}

		static void PubSubDevice_Client(CancellationToken cancellus, string name)
		{
			using (var socket = ZSocket.Create(context, ZSocketType.SUB))
			{
				socket.Connect(Backend);

				socket.SubscribeAll();

				ZError error;
				ZMessage message;
				var poller = ZPollItem.CreateReceiver(socket);

				while (!cancellus.IsCancellationRequested)
				{
					if (!poller.PollIn(out message, out error, TimeSpan.FromMilliseconds(64)))
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