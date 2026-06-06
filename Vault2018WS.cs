using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using start;
using WebSocketSharp;
using WebSocketSharp.Server;
using ws;

namespace vaultgamesesh
{
	public class Late2018WebSock
	{
		public Late2018WebSock()
		{
			string wsUrl = $"ws://{server.ServerConfig.ServerIP}:{server.ServerConfig.WebSocketPort}/";
			Late2018WebSock.instance = this;
			this.WebSock = new WebSocketServer(wsUrl);
			this.WebSock.AddWebSocketService<Late2018WebSock.NotificationWS>("/api/notification/v2");
			this.WebSock.AddWebSocketService<Late2018WebSock.HubWS>("/hub/v1");
			this.WebSock.Start();
			
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[Late2018WebSocket] Listening on {wsUrl}");
			Console.ResetColor();
		}

		public void Broadcast(Notification.Reponse res)
		{
			try
			{
				int clientCount = this.WebSock.WebSocketServices["/api/notification/v2"].Sessions.Count;
				Console.WriteLine($"[Late2018WebSocket] Broadcasting to {clientCount} clients.");
				WebSock.WebSocketServices["/api/notification/v2"].Sessions.Broadcast(
					Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(res)));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Late2018WebSocket] Broadcast error: {ex.Message}");
			}
		}

		public static Late2018WebSock instance;
		public WebSocketServer WebSock;

		public class HubWS : WebSocketBehavior
		{
			protected override void OnMessage(MessageEventArgs e)
			{
				if (server.ServerConfig.Debug)
					Console.WriteLine("[Late2018WS] Hub Requested.");
				base.Send(JsonConvert.SerializeObject(new Late2018WebSock.Hub()));
			}
		}

		public class Hub : WebSocketBehavior
		{
			public Hub()
			{
				this.accessToken = "RecUnitedRevivalToken";
				this.SupportedTransports = new List<string>();
				this.negotiateVersion = 0;
				this.url = new Uri($"http://{server.ServerConfig.PublicIP}:{server.ServerConfig.APIPort}/");
			}

			public Uri url { get; set; }
			public string accessToken { get; set; }
			public List<string> SupportedTransports { get; set; }
			public int negotiateVersion { get; set; }
		}

		public class NotificationWS : WebSocketBehavior
		{
			protected override void OnOpen()
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[Late2018WS] Client connected from {Context.UserEndPoint}");
				Console.ResetColor();
			}

			protected override void OnMessage(MessageEventArgs p0)
			{
				if (server.ServerConfig.Debug)
					Console.WriteLine("[Late2018WS] Notification Requested.");

				if (p0.Data == null)
				{
					base.Send(string.Empty);
				}
				else
				{
					base.Send(Notification2018.ProcessRequest(p0.Data));
				}
			}

			protected override void OnClose(CloseEventArgs e)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"[Late2018WS] Client disconnected.");
				Console.ResetColor();
			}

			protected override void OnError(WebSocketSharp.ErrorEventArgs e)
			{
				Console.WriteLine($"[Late2018WS] Error: {e.Message}");
			}
		}
	}
}
