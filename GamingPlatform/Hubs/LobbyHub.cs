using GamingPlatform.Models;
using GamingPlatform.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Linq;

namespace GamingPlatform.Hubs
{
    public class LobbyHub : Hub
    {
        private readonly LobbyService _lobbyService;
        private readonly Morpion.GameState _morpionGameState;
        private readonly Puissance4.GameState _puissance4GameState;

        public LobbyHub(
            LobbyService lobbyService,
            Morpion.GameState morpionGameState,
            Puissance4.GameState puissance4GameState)
        {
            _lobbyService = lobbyService;
            _morpionGameState = morpionGameState;
            _puissance4GameState = puissance4GameState;
        }

        public async Task JoinLobbyGroup(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        }

        public async Task LeaveLobbyGroup(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        public async Task RequestRematch(string lobbyId, string playerName)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            lobby.RematchVotes.Add(playerName);

            await Clients.Group(lobbyId).SendAsync("RematchVoteReceived", new
            {
                playerName = playerName,
                votesCount = lobby.RematchVotes.Count,
                totalPlayers = lobby.Players.Count,
                votes = lobby.RematchVotes.ToList()
            });

            if (lobby.RematchVotes.Count == lobby.Players.Count)
            {
                await StartRematch(lobbyId);
            }
        }

        public async Task DeclineRematch(string lobbyId, string playerName)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            lobby.RematchDeclined.Add(playerName);

            await Clients.Group(lobbyId).SendAsync("RematchDeclined", new
            {
                playerName = playerName,
                message = $"{playerName} a refusé la revanche."
            });

            int minPlayers = lobby.GameType == "SpeedTyping" ? 1 : 2;
            int remainingPlayers = lobby.Players.Count - lobby.RematchDeclined.Count;

            if (remainingPlayers < minPlayers)
            {
                CleanupOldGame(lobbyId, lobby.GameType);

                await Clients.Group(lobbyId).SendAsync("ReturnToLobby", new
                {
                    reason = "not_enough_players",
                    message = "Pas assez de joueurs pour rejouer. Retour au lobby."
                });
            }
        }

        public async Task ReturnAllToLobby(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            CleanupOldGame(lobbyId, lobby.GameType);
            lobby.ResetForRematch();

            await Clients.Group(lobbyId).SendAsync("ReturnToLobby", new
            {
                reason = "manual",
                message = "Retour au lobby."
            });
        }

        private async Task StartRematch(string lobbyId)
        {
            var oldLobby = _lobbyService.GetLobby(lobbyId);
            if (oldLobby == null) return;

            var gameName = oldLobby.Name;
            var gameType = oldLobby.GameType;
            var maxPlayers = oldLobby.MaxPlayers;
            var playersWhoVotedYes = oldLobby.RematchVotes.ToList();

            var newHost = playersWhoVotedYes.FirstOrDefault() ?? oldLobby.HostName;

            CleanupOldGame(lobbyId, gameType);
            _lobbyService.RemoveLobby(lobbyId);

            var newLobby = _lobbyService.CreateLobby(gameName, newHost, gameType, maxPlayers);

            foreach (var player in playersWhoVotedYes.Where(p => p != newHost))
            {
                _lobbyService.JoinLobby(newLobby.Id, player);
            }

            await Clients.Group(lobbyId).SendAsync("RematchStarting", new
            {
                message = "Tous les joueurs ont accepté ! La nouvelle partie commence..."
            });

            await Task.Delay(1500);

            await Clients.Group(lobbyId).SendAsync("GoToRoom", new
            {
                newLobbyId = newLobby.Id,
                gameType = gameType
            });
        }

        private void CleanupOldGame(string lobbyId, string gameType)
        {
            switch (gameType)
            {
                case "Morpion":
                    _morpionGameState.RemoveGameIfExists(lobbyId);
                    break;
                case "Puissance4":
                    _puissance4GameState.RemoveGameIfExists(lobbyId);
                    break;
            }
        }
        
        public async Task StartGame(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            if (lobby.GameType == "SpeedTyping")
            {
                if (lobby.GameState == null)
                {
                    var speedTypingGame = new Models.SpeedTypingGame(lobby.HostName);
                    foreach (var player in lobby.Players)
                    {
                        speedTypingGame.Players.Add(player);
                    }
                    lobby.GameState = speedTypingGame;
                }
                
                if (lobby.GameState is Models.SpeedTypingGame game)
                {
                    game.StartGame();
                }
            }
            else if (lobby.GameType == "Puissance4" && lobby.GameState == null)
            {
                if (lobby.Players.Count < 2)
                {
                    await Clients.Caller.SendAsync("Error", "Il faut 2 joueurs pour Puissance4");
                    return;
                }

                var player1 = new Models.Puissance4.Player(lobby.Players[0], lobby.Players[0]);
                var player2 = new Models.Puissance4.Player(lobby.Players[1], lobby.Players[1]);
                var game = new Models.Puissance4.Game(player1, player2);
                lobby.GameState = game;
            }

            if (_lobbyService.StartGame(lobbyId))
            {
                await Clients.Group(lobbyId).SendAsync("GameStarted", lobby.GameType);
            }
        }
    }
}
