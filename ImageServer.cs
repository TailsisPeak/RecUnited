using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using api;

namespace server
{
	// ImageServer serves profile images and room images to connected clients
	internal class ImageServer
	{
		public ImageServer()
		{
			try
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[ImageServer] Starting...");
				Console.ResetColor();
				new Thread(new ThreadStart(this.StartListen)) { IsBackground = true }.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine("An Exception Occurred while Listening :" + ex.ToString());
			}
		}

		private void StartListen()
		{
			// Try wildcard prefix; fall back to localhost on access denied.
			string prefix = $"http://+:{ServerConfig.ImageServerPort}/";
			this.listener.Prefixes.Add(prefix);
			try
			{
				this.listener.Start();

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"[ImageServer] Listening on {prefix}");
				Console.ResetColor();
			}
			catch (System.Net.HttpListenerException ex)
			{
				Console.WriteLine("[ImageServer] Start failed: " + ex.Message);
				// Fallback to localhost so non-elevated runs still work
				try
				{
					this.listener.Prefixes.Clear();
				}
				catch (ObjectDisposedException)
				{
					// Recreate listener if it was disposed
					this.listener = new HttpListener();
				}

				string fallback = $"http://localhost:{ServerConfig.ImageServerPort}/";
				try
				{
					this.listener.Prefixes.Add(fallback);
					this.listener.Start();

					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine($"[ImageServer] Fallback listening on {fallback}");
					Console.ResetColor();
				}
				catch (Exception startEx)
				{
					Console.WriteLine("[ImageServer] Fallback start failed: " + startEx.Message);
				}
			}

			while (true)
			{
				try
				{
					HttpListenerContext context = this.listener.GetContext();
					ThreadPool.QueueUserWorkItem(_ =>
					{
						try
						{
							HandleImageRequest(context);
						}
						catch (Exception ex)
						{
							Console.WriteLine("[ImageServer] Request error: " + ex.Message);
							try { context.Response.OutputStream.Close(); } catch { }
						}
					});
				}
				catch (Exception ex)
				{
					Console.WriteLine("[ImageServer] Listener error: " + ex.Message);
					Thread.Sleep(1000);
				}
			}
		}

		private void HandleImageRequest(HttpListenerContext context)
		{
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			string rawUrl = request.RawUrl;

			byte[] imageBytes = null;
			string profileImagePath = Path.Combine("SaveData", "profileimage.png");
			string defaultImagePath = Path.Combine("SaveData", "profileimage.png");

			try
			{
				if (rawUrl.StartsWith("/alt/"))
				{
					if (File.Exists(profileImagePath))
						imageBytes = File.ReadAllBytes(profileImagePath);
				}
				else if (rawUrl.StartsWith("/" + GetUsername()))
				{
					if (File.Exists(profileImagePath))
						imageBytes = File.ReadAllBytes(profileImagePath);
				}
				else if (rawUrl.StartsWith("/CustomRoom.png"))
				{
					string imageNamePath = Path.Combine("SaveData", "Rooms", "Downloaded", "imagename.txt");
					if (File.Exists(imageNamePath))
					{
						try
						{
							imageBytes = new WebClient().DownloadData("https://img.rec.net/" + File.ReadAllText(imageNamePath));
						}
						catch
						{
							// Fallback to default
							try { imageBytes = new WebClient().DownloadData("https://img.rec.net/DefaultRoomImage.jpg"); } catch { }
						}
					}
				}
				else if (rawUrl.StartsWith("//room/") || rawUrl.StartsWith("//data/"))
				{
					try
					{
						imageBytes = new WebClient().DownloadData("https://cdn.rec.net" + rawUrl.Remove(0, 1));
					}
					catch
					{
						Console.WriteLine("[ImageServer] CDN image not found: " + rawUrl);
					}
				}
				else
				{
					try
					{
						imageBytes = new WebClient().DownloadData("https://img.rec.net" + rawUrl);
					}
					catch
					{
						if (ServerConfig.Debug)
							Console.WriteLine("[ImageServer] Image not found on img.rec.net: " + rawUrl);
					}
				}

				// Fallback to default profile image
				if (imageBytes == null)
				{
					if (File.Exists(defaultImagePath))
						imageBytes = File.ReadAllBytes(defaultImagePath);
					else
						imageBytes = Encoding.UTF8.GetBytes(""); // Empty response as last resort
				}

				response.ContentLength64 = imageBytes.Length;
				response.ContentType = "image/png";
				response.Headers.Add("Access-Control-Allow-Origin", "*");
				response.OutputStream.Write(imageBytes, 0, imageBytes.Length);
				response.OutputStream.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine("[ImageServer] Error: " + ex.Message);
				try { response.OutputStream.Close(); } catch { }
			}
		}

		private string GetUsername()
		{
			try
			{
				string path = Path.Combine("SaveData", "Profile", "username.txt");
				if (File.Exists(path))
					return File.ReadAllText(path);
			}
			catch { }
			return "Player";
		}

		private HttpListener listener = new HttpListener();
	}
}
