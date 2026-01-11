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

        // Résultat de la tentative de rejoindre un lobby
        public enum JoinResult
        {
            Success,
            LobbyNotFound,
            GameAlreadyStarted,
            LobbyFull,
            NameAlreadyTaken
        }

        public JoinResult JoinLobby(string lobbyId, string playerName)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return JoinResult.LobbyNotFound;
            if (lobby.IsStarted) return JoinResult.GameAlreadyStarted;
            if (lobby.Players.Count >= lobby.MaxPlayers) return JoinResult.LobbyFull;
            
            // Vérifier si le nom est déjà pris (comparaison insensible à la casse)
            if (lobby.Players.Any(p => p.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
                return JoinResult.NameAlreadyTaken;

            lobby.Players.Add(playerName);
            return JoinResult.Success;
        }

        public void RemoveLobby(string lobbyId)
        {
            _lobbies.TryRemove(lobbyId, out _);
        }

        public bool RemovePlayerFromLobby(string lobbyId, string playerName)
        {
            if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return false;
            return lobby.Players.Remove(playerName);
        }

        public void ResetLobby(string lobbyId)
        {
            if (_lobbies.TryGetValue(lobbyId, out var lobby))
            {
                lobby.ResetForRematch();
            }
        }
        
        public bool StartGame(string lobbyId)
        {
             if (!_lobbies.TryGetValue(lobbyId, out var lobby)) return false;
             lobby.IsStarted = true;
             return true;
        }
    }
}
