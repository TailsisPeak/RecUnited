using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using api;
using api2018;
using api2017;
using Newtonsoft.Json;
using vaultgamesesh;
using System.Collections.Generic;

namespace server
{
	internal class APIServer
	{
		public static ulong CachedPlayerID = 10000000;
		public static ulong CachedPlatformID = 10000;
		public static int CachedVersionMonth = 01;

		// Per-request player tracking (thread-local for concurrent requests)
		[ThreadStatic]
		private static ulong _currentRequestPlayerId;

		public APIServer()
		{
			try
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"[APIServer] Starting on port {ServerConfig.APIPort}...");
				Console.ResetColor();
				new Thread(new ThreadStart(this.StartListen)) { IsBackground = true }.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine("An Exception Occurred while Listening :" + ex.ToString());
			}
		}

		private void HandleRequest(HttpListenerContext context)
		{
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;

			try
			{
				string rawUrl = request.RawUrl;
				string Url = "";
				string text = "";

				if (rawUrl.StartsWith("/api/"))
				{
					Url = rawUrl.Remove(0, 5);
				}

				using (StreamReader streamReader = new StreamReader(request.InputStream, request.ContentEncoding))
				{
					text = streamReader.ReadToEnd();
				}

				string remoteIP = request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
				
				if (ServerConfig.Debug)
				{
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.WriteLine($"[API] {remoteIP} -> {Url}");
					if (!string.IsNullOrEmpty(text))
						Console.WriteLine($"[API] Data: {text}");
					Console.ResetColor();
				}

				string s = ProcessAPIRequest(Url, rawUrl, text, request);

				if (ServerConfig.Debug && !string.IsNullOrEmpty(s) && s.Length < 500)
				{
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.WriteLine($"[API] Response: {s}");
					Console.ResetColor();
				}

				byte[] bytes = Encoding.UTF8.GetBytes(s);
				response.ContentLength64 = bytes.Length;
				response.ContentType = "application/json";
				
				// Add CORS headers for online access
				response.Headers.Add("Access-Control-Allow-Origin", "*");
				response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
				response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-RNSIG, Authorization");
				
				response.OutputStream.Write(bytes, 0, bytes.Length);
				response.OutputStream.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine("[API] HandleRequest error: " + ex.Message);
				try
				{
					response.StatusCode = 500;
					response.OutputStream.Close();
				}
				catch { }
			}
		}

		private string ProcessAPIRequest(string Url, string rawUrl, string text, HttpListenerRequest request)
		{
			string s = "";

			try
			{
				if (Url.StartsWith("versioncheck"))
				{
					if (Url.Contains("201809"))
					{
						CachedVersionMonth = 09;
					}
					else
					{
						CachedVersionMonth = 05;
					}
					s = VersionCheckResponse;
				}
				else if (Url == "config/v2")
				{
					s = Config2.GetDebugConfig();
				}
				else if (Url == "platformlogin/v1/getcachedlogins")
				{
					try
					{
						ulong playerId = ulong.Parse(text.Remove(0, 32));
						ulong platformId = ulong.Parse(text.Remove(0, 22));
						CachedPlayerID = playerId;
						CachedPlatformID = platformId;

						// Register player in database
						var player = PlayerDatabase.GetOrCreatePlayer(playerId);
						PlayerDatabase.PlayerConnected(playerId);

						s = getcachedlogins.GetDebugLogin(playerId, platformId);
					}
					catch (Exception ex)
					{
						Console.WriteLine("[API] Login parse error: " + ex.Message);
						s = getcachedlogins.GetDebugLogin(CachedPlayerID, CachedPlatformID);
					}
				}
				else if (Url == "platformlogin/v1/loginaccount" ||
						 Url == "platformlogin/v1/createaccount" ||
						 Url == "platformlogin/v1/logincached")
				{
					s = logincached.loginCache(CachedPlayerID, CachedPlatformID);
				}
				else if (Url == "relationships/v1/bulkignoreplatformusers")
				{
					s = BlankResponse;
				}
				else if (Url == "players/v1/list")
				{
					// Return list of online players
					var onlinePlayers = PlayerDatabase.GetOnlinePlayers();
					s = JsonConvert.SerializeObject(onlinePlayers);
				}
				else if (Url == "config/v1/amplitude")
				{
					s = Amplitude.amplitude();
				}
				else if (Url == "images/v2/named")
				{
					s = ImagesV2Named;
				}
				else if (Url == "PlayerReporting/v1/moderationBlockDetails")
				{
					s = ModerationBlockDetails;
				}
				else if (Url == "//api/chat/v2/myChats?mode=0&count=50" || rawUrl == "//api/chat/v2/myChats?mode=0&count=50")
				{
					s = BracketResponse;
				}
				else if (Url == "messages/v2/get")
				{
					s = BracketResponse;
				}
				else if (Url == "relationships/v2/get")
				{
					s = BracketResponse;
				}
				else if (Url == "gameconfigs/v1/all")
				{
					s = File.ReadAllText(Path.Combine("SaveData", "gameconfigs.txt"));
				}
				else if (Url.StartsWith("storefronts/v3/giftdropstore"))
				{
					s = BracketResponse;
				}
				else if (Url.StartsWith("storefronts/v3/balance/"))
				{
					s = BracketResponse;
				}
				else if (Url == "avatar/v2")
				{
					// Per-player avatar data
					var player = PlayerDatabase.GetPlayer(CachedPlayerID);
					if (player != null && !string.IsNullOrEmpty(player.AvatarData))
					{
						s = player.AvatarData;
					}
					else
					{
						s = File.ReadAllText(Path.Combine("SaveData", "avatar.txt"));
					}
				}
				else if (Url == "avatar/v2/saved")
				{
					s = BracketResponse;
				}
				else if (Url == "avatar/v2/set")
				{
					// Save per-player avatar
					if (!text.Contains("FaceFeatures"))
					{
						string faceFeaturesPath = Path.Combine("SaveData", "App", "facefeaturesadd.txt");
						if (File.Exists(faceFeaturesPath))
						{
							text = text.Remove(text.Length - 1, 1) + File.ReadAllText(faceFeaturesPath);
						}
					}
					var player = PlayerDatabase.GetPlayer(CachedPlayerID);
					if (player != null)
					{
						player.AvatarData = text;
						PlayerDatabase.UpdatePlayer(player);
					}
					File.WriteAllText(Path.Combine("SaveData", "avatar.txt"), text);
				}
				else if (Url == "settings/v2/")
				{
					var player = PlayerDatabase.GetPlayer(CachedPlayerID);
					if (player != null && !string.IsNullOrEmpty(player.SettingsData))
					{
						s = player.SettingsData;
					}
					else
					{
						s = File.ReadAllText(Path.Combine("SaveData", "settings.txt"));
					}
				}
				else if (Url == "settings/v2/set")
				{
					var player = PlayerDatabase.GetPlayer(CachedPlayerID);
					if (player != null)
					{
						player.SettingsData = text;
						PlayerDatabase.UpdatePlayer(player);
					}
					Settings.SetPlayerSettings(text);
				}
				else if (Url == "playersubscriptions/v1/my")
				{
					s = BracketResponse;
				}
				else if (Url == "avatar/v3/items")
				{
					s = File.ReadAllText(Path.Combine("SaveData", "avataritems2.txt"));
				}
				else if (Url == "equipment/v1/getUnlocked")
				{
					s = File.ReadAllText(Path.Combine("SaveData", "equipment.txt"));
				}
				else if (Url == "avatar/v1/saved")
				{
					s = BracketResponse;
				}
				else if (Url == "consumables/v1/getUnlocked")
				{
					if (CachedVersionMonth == 09)
					{
						s = BracketResponse;
					}
					else
					{
						s = File.ReadAllText(Path.Combine("SaveData", "consumables.txt"));
					}
				}
				else if (Url == "avatar/v2/gifts")
				{
					s = BracketResponse;
				}
				else if (Url == "storefronts/v2/2")
				{
					s = BlankResponse;
				}
				else if (Url == "storefronts/v1/allGiftDrops/2")
				{
					s = BracketResponse;
				}
				else if (Url == "objectives/v1/myprogress")
				{
					s = JsonConvert.SerializeObject(new Objective2018());
				}
				else if (Url == "rooms/v1/myrooms")
				{
					s = File.ReadAllText(Path.Combine("SaveData", "myrooms.txt"));
				}
				else if (Url == "rooms/v2/myrooms")
				{
					s = BracketResponse;
				}
				else if (Url == "rooms/v2/baserooms")
				{
					s = File.ReadAllText(Path.Combine("SaveData", "baserooms.txt"));
				}
				else if (Url == "rooms/v1/mybookmarkedrooms")
				{
					s = BracketResponse;
				}
				else if (Url == "rooms/v1/myRecent?skip=0&take=10")
				{
					s = BracketResponse;
				}
				else if (Url == "events/v3/list")
				{
					s = Events.list();
				}
				else if (Url == "playerevents/v1/all")
				{
					s = PlayerEventsResponse;
				}
				else if (Url == "activities/charades/v1/words")
				{
					s = Activities.Charades.words();
				}
				else if (Url == "gamesessions/v2/joinrandom")
				{
					s = gamesesh.GameSessions.JoinRandom(text);
				}
				else if (Url == "gamesessions/v2/create")
				{
					s = gamesesh.GameSessions.Create(text);
				}
				else if (Url == "gamesessions/v3/joinroom")
				{
					s = JsonConvert.SerializeObject(c000041.m000030(text));
				}
				else if (rawUrl == "//api/sanitize/v1/isPure")
				{
					s = "{\"IsPure\":true}";
				}
				else if (Url == "avatar/v3/saved")
				{
					s = BracketResponse;
				}
				else if (Url == "checklist/v1/current")
				{
					s = ChecklistV1Current;
				}
				else if (Url == "presence/v1/setplayertype")
				{
					s = BracketResponse;
				}
				else if (Url == "challenge/v1/getCurrent")
				{
					s = ChallengesV1GetCurrent;
				}
				else if (Url == "rooms/v1/featuredRoomGroup")
				{
					s = BracketResponse;
				}
				else if (Url == "rooms/v1/clone")
				{
					s = JsonConvert.SerializeObject(c000099.m00000a(text));
				}
				else if (Url.StartsWith("rooms/v2/saveData"))
				{
					// Room save data placeholder
					s = BlankResponse;
				}
				else if (Url == "presence/v3/heartbeat")
				{
					// Online heartbeat - track player activity
					s = JsonConvert.SerializeObject(Notification2018.Reponse.createResponse(4, c000020.m000027()));
				}
				else if (Url.StartsWith("rooms/v1/hot"))
				{
					// Return local room list instead of fetching from dead servers
					s = BracketResponse;
				}
				else if (Url.StartsWith("rooms/v2/instancedetails"))
				{
					s = BracketResponse;
				}
				else if (Url.StartsWith("rooms/v2/search?value="))
				{
					CustomRooms.RoomGet(Url.Remove(0, 22));
					s = BracketResponse;
				}
				else if (Url == "rooms/v4/details/29")
				{
					string roomDetailsPath = Path.Combine("SaveData", "Rooms", "Downloaded", "RoomDetails.json");
					if (File.Exists(roomDetailsPath))
					{
						s = File.ReadAllText(roomDetailsPath);
					}
					else
					{
						s = BracketResponse;
					}
				}
				else if (Url.StartsWith("rooms/v4/details"))
				{
					s = JsonConvert.SerializeObject(c00005d.m000023(Convert.ToInt32(Url.Remove(0, 17))));
				}
				else if (Url == "images/v1/slideshow")
				{
					s = BracketResponse;
				}
				// Server status API (new for online server)
				else if (Url == "server/v1/status")
				{
					s = JsonConvert.SerializeObject(new
					{
						ServerName = ServerConfig.ServerName,
						Motd = ServerConfig.Motd,
						OnlinePlayers = PlayerDatabase.OnlineCount,
						MaxPlayers = ServerConfig.MaxPlayers,
						ActiveSessions = GameSessionManager.ActiveSessionCount,
						Uptime = (DateTime.UtcNow - _startTime).ToString(@"dd\.hh\:mm\:ss")
					});
				}
				else
				{
					s = BracketResponse;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[API] Error processing {Url}: {ex.Message}");
				s = BracketResponse;
			}

			return s;
		}

		private void StartListen()
		{
			while (true)
			{
				try
				{
					// Ensure listener is fresh (recreate if previously closed/disposed)
					if (this.listener == null || !this.listener.IsListening)
					{
						try { this.listener?.Close(); } catch { }
						this.listener = new HttpListener();
					}

					string prefix = $"http://+:{ServerConfig.APIPort}/";
					if (!this.listener.Prefixes.Contains(prefix))
						this.listener.Prefixes.Add(prefix);

					this.listener.Start();
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"[APIServer] Listening on {prefix}");
					Console.ResetColor();

					while (this.listener.IsListening) 
					{
						try
						{
							var context = this.listener.GetContext();

							// Handle OPTIONS preflight requests for CORS
							if (context.Request.HttpMethod == "OPTIONS")
							{
								context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
								context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
								context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-RNSIG, Authorization");
								context.Response.StatusCode = 204;
								context.Response.OutputStream.Close();
								continue;
							}

							ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
						}
						catch (Exception ex)
						{
							Console.WriteLine("[APIServer] Request error: " + ex.Message);
						}
					}
				}
				catch (ObjectDisposedException)
				{
					// Listener was disposed elsewhere; recreate and retry
					Thread.Sleep(2000);
					continue;
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"[APIServer] Fatal error: {ex.Message}");
					Console.ResetColor();
					Thread.Sleep(2000);
					// loop will retry
				}
			}
		}

		private static DateTime _startTime = DateTime.UtcNow;

		public static string BlankResponse = "";
		public static string BracketResponse = "[]";
		public static string PlayerEventsResponse = "{\"Created\":[],\"Responses\":[]}";
		public static string VersionCheckResponse2 = "{\"VersionStatus\":0}";
		public static string VersionCheckResponse = "{\"ValidVersion\":true}";
		public static string ModerationBlockDetails = "{\"ReportCategory\":0,\"Duration\":0,\"GameSessionId\":0,\"Message\":\"\"}";
		public static string ImagesV2Named = "[{\"FriendlyImageName\":\"DormRoomBucket\",\"ImageName\":\"DormRoomBucket\",\"StartTime\":\"2021-12-27T21:27:38.1880175-08:00\",\"EndTime\":\"2030-12-27T21:27:38.1880399-08:00\"}]";
		public static string ChallengesV1GetCurrent = "{\"Success\":true,\"Message\":\"RecUnited Revival\"}";
		public static string ChecklistV1Current = "[{\"Order\":0,\"Objective\":3000,\"Count\":3,\"CreditAmount\":100},{\"Order\":1,\"Objective\":3001,\"Count\":3,\"CreditAmount\":100},{\"Order\":2,\"Objective\":3002,\"Count\":3,\"CreditAmount\":100}]";

		private HttpListener listener = new HttpListener();
	}
}
