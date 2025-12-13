using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GamingPlatform.Models;

namespace GamingPlatform.Services
{
    public class LobbyService
    {
        private readonly ConcurrentDictionary<string, GameLobby> _lobbies = new();

        public GameLobby CreateLobby(string name, string hostName, string gameType, int maxPlayers = 2)
        {
            var lobby = new GameLobby
            {
                Name = name,
                HostName = hostName,
                GameType = gameType,
                MaxPlayers = maxPlayers
            };
            lobby.Players.Add(hostName);
            _lobbies.TryAdd(lobby.Id, lobby);
            return lobby;
        }

        public GameLobby GetLobby(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId)) return null;
            _lobbies.TryGetValue(lobbyId, out var lobby);
            return lobby;
        }

        public IEnumerable<GameLobby> GetLobbies(string gameType)
        {
            return _lobbies.Values.Where(l => l.GameType == gameType && !l.IsStarted);
        }

        public bool JoinLobby(string lobbyId, string playerName)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return false;
            if (lobby.IsStarted || lobby.Players.Count >= lobby.MaxPlayers) return false;
            if (lobby.Players.Contains(playerName)) return true; // Already joined

            lobby.Players.Add(playerName);
            return true;
        }

        public void RemoveLobby(string lobbyId)
        {
            _lobbies.TryRemove(lobbyId, out _);
        }
        
        public bool StartGame(string lobbyId)
        {
             if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return false;
             lobby.IsStarted = true;
             return true;
        }
    }
}
