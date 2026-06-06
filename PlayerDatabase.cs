using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace server
{
    /// <summary>
    /// Multi-player account database backed by a JSON file.
    /// Supports concurrent access from multiple API threads.
    /// Each connecting player gets their own profile, avatar, settings, etc.
    /// </summary>
    public static class PlayerDatabase
    {
        private static readonly string DbPath = Path.Combine("SaveData", "players.json");
        private static readonly object _lock = new object();
        private static ConcurrentDictionary<ulong, PlayerAccount> _players = new ConcurrentDictionary<ulong, PlayerAccount>();
        private static ConcurrentDictionary<ulong, ConnectedPlayer> _connectedPlayers = new ConcurrentDictionary<ulong, ConnectedPlayer>();

        public static void Load()
        {
            lock (_lock)
            {
                if (File.Exists(DbPath))
                {
                    try
                    {
                        string json = File.ReadAllText(DbPath);
                        var dict = JsonConvert.DeserializeObject<Dictionary<ulong, PlayerAccount>>(json);
                        if (dict != null)
                        {
                            _players = new ConcurrentDictionary<ulong, PlayerAccount>(dict);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PlayerDatabase] Error loading: {ex.Message}");
                    }
                }
                Console.WriteLine($"[PlayerDatabase] Loaded {_players.Count} player accounts.");
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(_players, Formatting.Indented);
                    File.WriteAllText(DbPath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PlayerDatabase] Error saving: {ex.Message}");
                }
            }
        }

        public static PlayerAccount GetOrCreatePlayer(ulong platformId)
        {
            if (_players.TryGetValue(platformId, out PlayerAccount existing))
            {
                return existing;
            }

            // Create new player
            var newPlayer = new PlayerAccount
            {
                PlayerId = platformId,
                Username = "Player#" + new Random().Next(0, 999999),
                DisplayName = "Player#" + new Random().Next(0, 999999),
                Level = 10,
                XP = 9999,
                Developer = false,
                ProfileImageName = "default",
                CreatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            // Load defaults from SaveData if they exist (first player gets the legacy data)
            if (_players.Count == 0)
            {
                try
                {
                    if (File.Exists(Path.Combine("SaveData", "Profile", "username.txt")))
                        newPlayer.Username = File.ReadAllText(Path.Combine("SaveData", "Profile", "username.txt"));
                    if (File.Exists(Path.Combine("SaveData", "Profile", "level.txt")))
                        newPlayer.Level = int.Parse(File.ReadAllText(Path.Combine("SaveData", "Profile", "level.txt")));
                }
                catch { }
            }

            newPlayer.DisplayName = newPlayer.Username;
            _players[platformId] = newPlayer;
            Save();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[PlayerDatabase] New player registered: {newPlayer.Username} (ID: {platformId})");
            Console.ResetColor();

            return newPlayer;
        }

        public static PlayerAccount GetPlayer(ulong playerId)
        {
            _players.TryGetValue(playerId, out PlayerAccount player);
            return player;
        }

        public static void UpdatePlayer(PlayerAccount player)
        {
            _players[player.PlayerId] = player;
            Save();
        }

        // Connected player tracking
        public static void PlayerConnected(ulong playerId)
        {
            var player = GetPlayer(playerId);
            if (player != null)
            {
                player.LastSeen = DateTime.UtcNow;
                _connectedPlayers[playerId] = new ConnectedPlayer
                {
                    PlayerId = playerId,
                    Username = player.Username,
                    ConnectedAt = DateTime.UtcNow
                };
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[PlayerDatabase] Player connected: {player.Username} (ID: {playerId}) | Online: {_connectedPlayers.Count}");
                Console.ResetColor();
            }
        }

        public static void PlayerDisconnected(ulong playerId)
        {
            _connectedPlayers.TryRemove(playerId, out _);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[PlayerDatabase] Player disconnected (ID: {playerId}) | Online: {_connectedPlayers.Count}");
            Console.ResetColor();
        }

        public static int OnlineCount => _connectedPlayers.Count;

        public static List<ConnectedPlayer> GetOnlinePlayers()
        {
            return _connectedPlayers.Values.ToList();
        }

        public static List<PlayerAccount> GetAllPlayers()
        {
            return _players.Values.ToList();
        }
    }

    public class PlayerAccount
    {
        public ulong PlayerId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public int Level { get; set; }
        public int XP { get; set; }
        public bool Developer { get; set; }
        public string ProfileImageName { get; set; }
        public string AvatarData { get; set; }
        public string SettingsData { get; set; }
        public string EquipmentData { get; set; }
        public string ConsumablesData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsBanned { get; set; }
        public string BanReason { get; set; }
    }

    public class ConnectedPlayer
    {
        public ulong PlayerId { get; set; }
        public string Username { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
