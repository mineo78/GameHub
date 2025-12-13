using GamingPlatform.Models;
using GamingPlatform.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GamingPlatform.Hubs
{
    public class LobbyHub : Hub
    {
        private readonly LobbyService _lobbyService;

        public LobbyHub(LobbyService lobbyService)
        {
            _lobbyService = lobbyService;
        }

        public async Task JoinLobbyGroup(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        }

        public async Task LeaveLobbyGroup(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }
        
        public async Task StartGame(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            // Initialiser le GameState selon le type de jeu
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
                
                // Démarrer le jeu SpeedTyping pour générer le texte
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
