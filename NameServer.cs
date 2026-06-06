using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using start;

namespace server
{
	// NameServer tells the game client where to find the API, WebSocket, and Image servers
	internal class NameServer
	{
		public NameServer()
		{
			try
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[NameServer] Starting...");
				Console.ResetColor();
				new Thread(new ThreadStart(this.StartListen)) { IsBackground = true }.Start();
				new Thread(new ThreadStart(this.StartListen2)) { IsBackground = true }.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine("An Exception Occurred while Listening :" + ex.ToString());
			}
		}

		private void StartListen()
		{
			string prefix = $"http://127.0.0.1:{ServerConfig.NameServerPort}/";
			this.listener.Prefixes.Add(prefix);
			try
			{
				this.listener.Start();
			} catch (Exception ex)
			{
				Console.WriteLine("An Exception occured while trying to setup the NameServer listener: " + ex.ToString());
				Console.WriteLine("Press any key to quit...");
				Console.ReadKey();
				Console.WriteLine("Code 1: Unhandled Exception");
                Environment.Exit(1);

            }


			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[NameServer] Listening on {prefix}");
			Console.ResetColor();

			while (true)
			{
				try
				{
					HttpListenerContext context = this.listener.GetContext();
					ThreadPool.QueueUserWorkItem(_ =>
					{
						try
						{
							HttpListenerResponse response = context.Response;
							
							// Point clients to the correct server URLs
							NSData data = new NSData()
							{
								API = ServerConfig.APIUrl,
								Notifications = ServerConfig.NotificationsUrl,
								Images = ServerConfig.ImagesUrl
							};
							
							string s = JsonConvert.SerializeObject(data);
							
							if (ServerConfig.Debug)
							{
								Console.ForegroundColor = ConsoleColor.DarkGray;
								Console.WriteLine($"[NameServer] Serving endpoints: API={data.API}, WS={data.Notifications}, IMG={data.Images}");
								Console.ResetColor();
							}
							
							byte[] bytes = Encoding.UTF8.GetBytes(s);
							response.ContentLength64 = (long)bytes.Length;
							response.ContentType = "application/json";
							response.Headers.Add("Access-Control-Allow-Origin", "*");
							response.OutputStream.Write(bytes, 0, bytes.Length);
							response.OutputStream.Close();
						}
						catch (Exception ex)
						{
							Console.WriteLine("[NameServer] Response error: " + ex.Message);
						}
					});
				}
				catch (Exception ex)
				{
					Console.WriteLine("[NameServer] Listener error: " + ex.Message);
					Thread.Sleep(1000);
				}
			}
		}

		private void StartListen2()
		{
			string prefix = $"http://{ServerConfig.ServerIP}:{ServerConfig.NameServer2Port}/";
			try
			{
				this.listener2.Prefixes.Add(prefix);
				this.listener2.Start();

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"[NameServer2] Listening on {prefix}");
				Console.ResetColor();

				while (true)
				{
					try
					{
						HttpListenerContext context = this.listener2.GetContext();
						ThreadPool.QueueUserWorkItem(_ =>
						{
							try
							{
								HttpListenerResponse response = context.Response;
								
								NSData data = new NSData()
								{
									API = ServerConfig.APIUrl,
									Notifications = ServerConfig.NotificationsUrl,
									Images = ServerConfig.ImagesUrl
								};
								
								string s = JsonConvert.SerializeObject(data);
								byte[] bytes = Encoding.UTF8.GetBytes(s);
								response.ContentLength64 = (long)bytes.Length;
								response.ContentType = "application/json";
								response.Headers.Add("Access-Control-Allow-Origin", "*");
								response.OutputStream.Write(bytes, 0, bytes.Length);
								response.OutputStream.Close();
							}
							catch (Exception ex)
							{
								Console.WriteLine("[NameServer2] Response error: " + ex.Message);
							}
						});
					}
					catch (Exception ex)
					{
						Console.WriteLine("[NameServer2] Listener error: " + ex.Message);
						Thread.Sleep(1000);
					}
				}
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"[NameServer2] Could not bind to port {ServerConfig.NameServer2Port}: {ex.Message}");
				Console.WriteLine("[NameServer2] This is normal if port 56 requires elevated privileges.");
				Console.ResetColor();
			}
		}

		public class NSData
		{
			public string API { get; set; }
			public string Notifications { get; set; }
			public string Images { get; set; }
		}

		private HttpListener listener = new HttpListener();
		private HttpListener listener2 = new HttpListener();
	}
}
