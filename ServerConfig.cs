using System;
using System.IO;
using Newtonsoft.Json;

namespace server
{
    /// <summary>
    /// Global server configuration loaded from Conf/config.json.
    /// Used by all server components to determine bind addresses and settings.
    /// </summary>
    public static class ServerConfig
    {
        public static int Port { get; private set; } = 2018;
        public static string ServerIP { get; private set; } = "0.0.0.0";
        public static string PublicIP { get; private set; } = "localhost";
        public static int MaxPlayers { get; private set; } = 20;
        public static bool Debug { get; private set; } = true;
        public static string ServerName { get; private set; } = "RecUnited Revival Server";
        public static string Motd { get; private set; } = "Welcome to RecUnited Revival!";

        // Derived ports
        public static int APIPort => Port;
        public static int NameServerPort => 20181;
        public static int NameServer2Port => 20183; // Changed from 56 to avoid privileged port requirement
        public static int ImageServerPort => 20182;
        public static int WebSocketPort => 20161;

        // Derived URLs (using PublicIP for client-facing, ServerIP for binding)
        public static string APIUrl => $"http://{PublicIP}:{APIPort}";
        public static string NotificationsUrl => $"http://{PublicIP}:{WebSocketPort}";
        public static string ImagesUrl => $"http://{PublicIP}:{ImageServerPort}";

        public static void Load()
        {
            try
            {
                string configPath = Path.Combine("Conf", "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<ConfigFile>(json);
                    if (config != null)
                    {
                        Port = config.port > 0 ? config.port : Port;
                        ServerIP = !string.IsNullOrEmpty(config.ServerIP) ? config.ServerIP : ServerIP;
                        PublicIP = !string.IsNullOrEmpty(config.PublicIP) ? config.PublicIP : PublicIP;
                        MaxPlayers = config.maxPlayers > 0 ? config.maxPlayers : MaxPlayers;
                        Debug = config.debug;
                        ServerName = !string.IsNullOrEmpty(config.serverName) ? config.serverName : ServerName;
                        Motd = !string.IsNullOrEmpty(config.motd) ? config.motd : Motd;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[ServerConfig] Loaded - Bind: {ServerIP}, Public: {PublicIP}, API Port: {APIPort}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ServerConfig] Error loading config: {ex.Message}");
                Console.WriteLine("[ServerConfig] Using default values.");
                Console.ResetColor();
            }
        }

        private class ConfigFile
        {
            public int port { get; set; }
            public string ServerIP { get; set; }
            public string PublicIP { get; set; }
            public int maxPlayers { get; set; }
            public bool debug { get; set; }
            public string serverName { get; set; }
            public string motd { get; set; }
        }
    }
}
