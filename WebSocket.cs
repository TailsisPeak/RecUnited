using System;
using System.IO;
using System.Net;
using start;
using WebSocketSharp;
using WebSocketSharp.Server;
using server;


namespace ws
{
	// WebSocket server for real-time notifications to connected clients
	internal class WebSocket
	{
		public WebSocket()
		{
			string wsUrl = $"ws://{ServerConfig.ServerIP}:{ServerConfig.WebSocketPort}";
			WebSocketServer webSocketServer = new WebSocketServer(wsUrl);
			webSocketServer.AddWebSocketService<WebSocket.NotificationV2>("/api/notification/v2");
			webSocketServer.AddWebSocketService<WebSocket.NotificationV2>("/hub/v1");
			webSocketServer.Start();
			
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[WebSocket] Listening on {wsUrl}");
			Console.ResetColor();
		}

		public class NotificationV2 : WebSocketBehavior
		{
			protected override void OnOpen()
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[WebSocket] Client connected from {Context.UserEndPoint}");
				Console.ResetColor();
			}

			protected override void OnMessage(MessageEventArgs e)
			{
				if (ServerConfig.Debug)
				{
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.WriteLine("[WebSocket] Message received: " + (e.Data?.Length > 200 ? e.Data.Substring(0, 200) + "..." : e.Data));
					Console.ResetColor();
				}
				base.Send(Notification.ProcessRequest(e.Data));
			}

			protected override void OnClose(CloseEventArgs e)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"[WebSocket] Client disconnected: {e.Reason}");
				Console.ResetColor();
			}

			protected override void OnError(WebSocketSharp.ErrorEventArgs e)
			{
				Console.WriteLine($"[WebSocket] Error: {e.Message}");
			}
		}
	}
}
