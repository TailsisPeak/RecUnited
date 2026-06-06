using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace server
{
    /// <summary>
    /// Manages multiplayer game sessions. Multiple players can join the same session,
    /// and multiple sessions can run concurrently.
    /// </summary>
    public static class GameSessionManager
    {
        private static readonly ConcurrentDictionary<long, OnlineGameSession> _sessions = new ConcurrentDictionary<long, OnlineGameSession>();
        private static long _nextSessionId = 1;

        public static OnlineGameSession FindOrCreateSession(string roomName, string activityLevelId, int maxCapacity = 20)
        {
            // Try to find an existing non-full, non-private session for this room
            var existing = _sessions.Values.FirstOrDefault(s =>
                s.RoomName == roomName &&
                !s.IsFull &&
                !s.IsPrivate &&
                s.PlayerCount < s.MaxCapacity);

            if (existing != null)
            {
                Console.WriteLine($"[GameSessionManager] Player joining existing session {existing.SessionId} for room '{roomName}' ({existing.PlayerCount}/{existing.MaxCapacity})");
                return existing;
            }

            // Create new session
            long id = System.Threading.Interlocked.Increment(ref _nextSessionId) + 20180;
            var session = new OnlineGameSession
            {
                SessionId = id,
                RoomName = roomName,
                ActivityLevelId = activityLevelId,
                MaxCapacity = maxCapacity,
                CreatedAt = DateTime.UtcNow
            };
            _sessions[id] = session;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[GameSessionManager] Created session {id} for room '{roomName}' (max: {maxCapacity})");
            Console.ResetColor();

            return session;
        }

        public static OnlineGameSession CreatePrivateSession(string roomName, string activityLevelId, ulong creatorId, int maxCapacity = 20)
        {
            long id = System.Threading.Interlocked.Increment(ref _nextSessionId) + 20180;
            var session = new OnlineGameSession
            {
                SessionId = id,
                RoomName = roomName,
                ActivityLevelId = activityLevelId,
                MaxCapacity = maxCapacity,
                IsPrivate = true,
                CreatorPlayerId = creatorId,
                CreatedAt = DateTime.UtcNow
            };
            _sessions[id] = session;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[GameSessionManager] Created PRIVATE session {id} for room '{roomName}' by player {creatorId}");
            Console.ResetColor();

            return session;
        }

        public static void AddPlayerToSession(long sessionId, ulong playerId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Players.Add(playerId);
                Console.WriteLine($"[GameSessionManager] Player {playerId} joined session {sessionId} ({session.PlayerCount}/{session.MaxCapacity})");
            }
        }

        public static void RemovePlayerFromSession(long sessionId, ulong playerId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Players.Remove(playerId);
                Console.WriteLine($"[GameSessionManager] Player {playerId} left session {sessionId} ({session.PlayerCount}/{session.MaxCapacity})");

                // Clean up empty sessions
                if (session.PlayerCount == 0)
                {
                    _sessions.TryRemove(sessionId, out _);
                    Console.WriteLine($"[GameSessionManager] Session {sessionId} removed (empty)");
                }
            }
        }

        public static OnlineGameSession GetSession(long sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public static List<OnlineGameSession> GetAllSessions()
        {
            return _sessions.Values.ToList();
        }

        public static int ActiveSessionCount => _sessions.Count;
        public static int TotalPlayersInSessions => _sessions.Values.Sum(s => s.PlayerCount);
    }

    public class OnlineGameSession
    {
        public long SessionId { get; set; }
        public string RoomName { get; set; }
        public string ActivityLevelId { get; set; }
        public int MaxCapacity { get; set; } = 20;
        public bool IsPrivate { get; set; }
        public ulong CreatorPlayerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public HashSet<ulong> Players { get; set; } = new HashSet<ulong>();

        [JsonIgnore]
        public int PlayerCount => Players.Count;
        [JsonIgnore]
        public bool IsFull => Players.Count >= MaxCapacity;
    }
}
