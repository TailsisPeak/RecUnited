using System;
using server;
using System.Net.Http;
using System.IO;
using ws;
using api;
using System.Net;
using System.Diagnostics;
using vaultgamesesh;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

namespace start
{
    class Program
    {
        static void Main()
        {
            Console.Title = "RecUnited Revival Server";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ██████╗ ███████╗ ██████╗    ██╗   ██╗███╗   ██╗██╗████████╗███████╗██████╗ 
  ██╔══██╗██╔════╝██╔════╝    ██║   ██║████╗  ██║██║╚══██╔══╝██╔════╝██╔══██╗
  ██████╔╝█████╗  ██║         ██║   ██║██╔██╗ ██║██║   ██║   █████╗  ██║  ██║
  ██╔══██╗██╔══╝  ██║         ██║   ██║██║╚██╗██║██║   ██║   ██╔══╝  ██║  ██║
  ██║  ██║███████╗╚██████╗    ╚██████╔╝██║ ╚████║██║   ██║   ███████╗██████╔╝
  ╚═╝  ╚═╝╚══════╝ ╚═════╝     ╚═════╝ ╚═╝  ╚═══╝╚═╝   ╚═╝   ╚══════╝╚═════╝ 
                         R E V I V A L   S E R V E R
");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Version: {appversion} | Online Multiplayer Server");
            Console.WriteLine($"  Made by The RecUnited Team");
            Console.WriteLine(new string('─', 70));
            Console.ResetColor();

            // Load server configuration
            ServerConfig.Load();

            // Setup data directories and default files
            Setup.setup();

            // Load player database
            PlayerDatabase.Load();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("(1) Start Server" + "(2) Credits");
            var str = Console.ReadLine();

            if (str == "2")
            {

                Console.WriteLine();
                Console.WriteLine("RecUnited Revival Server");
                Console.WriteLine("Version: " + appversion);
                Console.WriteLine("Made by The RecUnited Team");
                Console.WriteLine(); // Make this fetch a txt file from 
                string url = "https://raw.githubusercontent.com/TailsisPeak/RecUnited/refs/heads/master/Contrib/credits.txt";

                // Wrap async call in Task.Run and use .Result to execute synchronously
                string textContent = System.Threading.Tasks.Task.Run(async () =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        return await client.GetStringAsync(url);
                    }
                }).Result;

                Console.WriteLine(textContent);
                Console.WriteLine("Press any key to return to main menu.");
                Console.ReadKey();
                Main();
            }
            else
            {

                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║              STARTING ONLINE SERVER COMPONENTS               ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();


                // Set version for 2018 (main supported version)
                version = "2018";

                // Start all server components
                try
                {
                    new NameServer();
                    Console.WriteLine();

                    new ImageServer();
                    Console.WriteLine();

                    new APIServer();
                    Console.WriteLine();

                    // WebSocket for notifications
                    try
                    {
                        new Late2018WebSock();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[Late2018WS] Could not start (may conflict with WebSocket): {ex.Message}");
                        Console.WriteLine("[Late2018WS] Falling back to basic WebSocket...");
                        Console.ResetColor();
                        try
                        {
                            new ws.WebSocket();
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FATAL] Error starting server: {ex.Message}");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║            ✓ ALL SERVER COMPONENTS STARTED                   ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  Server Name:     {ServerConfig.ServerName}");
                Console.WriteLine($"  Bind Address:    {ServerConfig.ServerIP}");
                Console.WriteLine($"  Public Address:  {ServerConfig.PublicIP}");
                Console.WriteLine($"  API Port:        {ServerConfig.APIPort}2018");
                Console.WriteLine($"  NameServer Port: {ServerConfig.NameServerPort}");
                Console.WriteLine($"  Image Port:      {ServerConfig.ImageServerPort}");
                Console.WriteLine($"  WebSocket Port:  {ServerConfig.WebSocketPort}");
                Console.WriteLine($"  Max Players:     {ServerConfig.MaxPlayers}");
                Console.ResetColor();
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  Clients should connect to: " + ServerConfig.PublicIP);
                Console.ResetColor();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Type 'help' for server commands.");
                Console.ResetColor();
                Console.WriteLine(new string('─', 70));
                Console.WriteLine();

                // Server command loop
                RunServerCommandLoop();
            }

            static void RunServerCommandLoop()
            {
                while (true)
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write("server> ");
                        Console.ResetColor();
                        string input = Console.ReadLine()?.Trim().ToLower();

                        if (string.IsNullOrEmpty(input))
                            continue;

                        switch (input)
                        {
                            case "help":
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine();
                                Console.WriteLine("  Server Commands:");
                                Console.WriteLine("  ─────────────────────────────────");
                                Console.WriteLine("  status    - Show server status");
                                Console.WriteLine("  players   - List online players");
                                Console.WriteLine("  sessions  - List active game sessions");
                                Console.WriteLine("  accounts  - List all registered accounts");
                                Console.WriteLine("  kick <id> - Kick a player by ID");
                                Console.WriteLine("  ban <id>  - Ban a player by ID");
                                Console.WriteLine("  unban <id>- Unban a player by ID");
                                Console.WriteLine("  motd <msg>- Set message of the day");
                                Console.WriteLine("  save      - Force save player database");
                                Console.WriteLine("  quit      - Shutdown the server");
                                Console.WriteLine();
                                Console.ResetColor();
                                break;

                            case "status":
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine();
                                Console.WriteLine($"  ╔═ Server Status ═══════════════════════════╗");
                                Console.WriteLine($"  ║ Online Players:   {PlayerDatabase.OnlineCount,-24}║");
                                Console.WriteLine($"  ║ Registered:       {PlayerDatabase.GetAllPlayers().Count,-24}║");
                                Console.WriteLine($"  ║ Active Sessions:  {GameSessionManager.ActiveSessionCount,-24}║");
                                Console.WriteLine($"  ║ Server Name:      {ServerConfig.ServerName,-24}║");
                                Console.WriteLine($"  ╚═════════════════════════════════════════════╝");
                                Console.WriteLine();
                                Console.ResetColor();
                                break;

                            case "players":
                                var online = PlayerDatabase.GetOnlinePlayers();
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"\n  Online Players ({online.Count}):");
                                Console.WriteLine("  ─────────────────────────────────");
                                if (online.Count == 0)
                                {
                                    Console.WriteLine("  (none)");
                                }
                                else
                                {
                                    foreach (var p in online)
                                    {
                                        Console.WriteLine($"  [{p.PlayerId}] {p.Username} (connected {p.ConnectedAt:HH:mm:ss})");
                                    }
                                }
                                Console.WriteLine();
                                Console.ResetColor();
                                break;

                            case "sessions":
                                var sessions = GameSessionManager.GetAllSessions();
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine($"\n  Active Sessions ({sessions.Count}):");
                                Console.WriteLine("  ─────────────────────────────────");
                                if (sessions.Count == 0)
                                {
                                    Console.WriteLine("  (none)");
                                }
                                else
                                {
                                    foreach (var s in sessions)
                                    {
                                        Console.WriteLine($"  Session {s.SessionId}: {s.RoomName} ({s.PlayerCount}/{s.MaxCapacity}) {(s.IsPrivate ? "[PRIVATE]" : "")}");
                                    }
                                }
                                Console.WriteLine();
                                Console.ResetColor();
                                break;

                            case "accounts":
                                var allPlayers = PlayerDatabase.GetAllPlayers();
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine($"\n  Registered Accounts ({allPlayers.Count}):");
                                Console.WriteLine("  ─────────────────────────────────");
                                foreach (var p in allPlayers)
                                {
                                    string status = p.IsBanned ? " [BANNED]" : "";
                                    Console.WriteLine($"  [{p.PlayerId}] {p.Username} Lv.{p.Level}{status} (last seen: {p.LastSeen:yyyy-MM-dd HH:mm})");
                                }
                                Console.WriteLine();
                                Console.ResetColor();
                                break;

                            case "save":
                                PlayerDatabase.Save();
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("  Database saved.");
                                Console.ResetColor();
                                break;

                            case "quit":
                            case "exit":
                            case "stop":
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("  Saving database and shutting down...");
                                PlayerDatabase.Save();
                                Console.WriteLine("  Goodbye!");
                                Console.ResetColor();
                                Environment.Exit(0);
                                break;

                            default:
                                if (input.StartsWith("kick "))
                                {
                                    string idStr = input.Substring(5).Trim();
                                    if (ulong.TryParse(idStr, out ulong kickId))
                                    {
                                        PlayerDatabase.PlayerDisconnected(kickId);
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"  Player {kickId} kicked.");
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        Console.WriteLine("  Invalid player ID.");
                                    }
                                }
                                else if (input.StartsWith("ban "))
                                {
                                    string idStr = input.Substring(4).Trim();
                                    if (ulong.TryParse(idStr, out ulong banId))
                                    {
                                        var player = PlayerDatabase.GetPlayer(banId);
                                        if (player != null)
                                        {
                                            player.IsBanned = true;
                                            player.BanReason = "Banned by server admin";
                                            PlayerDatabase.UpdatePlayer(player);
                                            PlayerDatabase.PlayerDisconnected(banId);
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"  Player {player.Username} ({banId}) banned.");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.WriteLine("  Player not found.");
                                        }
                                    }
                                }
                                else if (input.StartsWith("unban "))
                                {
                                    string idStr = input.Substring(6).Trim();
                                    if (ulong.TryParse(idStr, out ulong unbanId))
                                    {
                                        var player = PlayerDatabase.GetPlayer(unbanId);
                                        if (player != null)
                                        {
                                            player.IsBanned = false;
                                            player.BanReason = "";
                                            PlayerDatabase.UpdatePlayer(player);
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"  Player {player.Username} ({unbanId}) unbanned.");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.WriteLine("  Player not found.");
                                        }
                                    }
                                }
                                else if (input.StartsWith("motd "))
                                {
                                    string newMotd = input.Substring(5).Trim();
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"  MOTD set to: {newMotd}");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine("  Unknown command. Type 'help' for a list of commands.");
                                    Console.ResetColor();
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Command error: {ex.Message}");
                    }
                }
            }
        }

        public static string msg = "Server is running and accepting connections.";
        public static string version = "";
        public static string appversion = "1.0.0-revival";
        public static bool bannedflag = false;
    }
}
