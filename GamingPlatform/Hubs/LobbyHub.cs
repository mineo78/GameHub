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

        // ========================
        // REMATCH SYSTEM
        // ========================

        /// <summary>
        /// Un joueur demande une revanche (vote pour rejouer)
        /// </summary>
        public async Task RequestRematch(string lobbyId, string playerName)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            // Ajouter le vote
            lobby.RematchVotes.Add(playerName);

            // Notifier tout le monde du vote
            await Clients.Group(lobbyId).SendAsync("RematchVoteReceived", new
            {
                playerName = playerName,
                votesCount = lobby.RematchVotes.Count,
                totalPlayers = lobby.Players.Count,
                votes = lobby.RematchVotes.ToList()
            });

            // Vérifier si tous les joueurs ont accepté
            if (lobby.RematchVotes.Count == lobby.Players.Count)
            {
                await StartRematch(lobbyId);
            }
        }

        /// <summary>
        /// Un joueur refuse la revanche
        /// </summary>
        public async Task DeclineRematch(string lobbyId, string playerName)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            lobby.RematchDeclined.Add(playerName);

            // Notifier tout le monde
            await Clients.Group(lobbyId).SendAsync("RematchDeclined", new
            {
                playerName = playerName,
                message = $"{playerName} a refusé la revanche."
            });

            // Vérifier s'il reste assez de joueurs pour jouer
            int minPlayers = lobby.GameType == "SpeedTyping" ? 1 : 2;
            int remainingPlayers = lobby.Players.Count - lobby.RematchDeclined.Count;

            if (remainingPlayers < minPlayers)
            {
                // Pas assez de joueurs, retourner au lobby
                await Clients.Group(lobbyId).SendAsync("ReturnToLobby", new
                {
                    reason = "not_enough_players",
                    message = "Pas assez de joueurs pour rejouer. Retour au lobby."
                });
            }
        }

        /// <summary>
        /// Retourne tout le monde au lobby (appelé par un joueur ou automatiquement)
        /// </summary>
        public async Task ReturnAllToLobby(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            // Reset le lobby pour une nouvelle partie
            lobby.ResetForRematch();

            await Clients.Group(lobbyId).SendAsync("ReturnToLobby", new
            {
                reason = "manual",
                message = "Retour au lobby."
            });
        }

        /// <summary>
        /// Démarre une nouvelle partie avec les mêmes joueurs
        /// </summary>
        private async Task StartRematch(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null) return;

            // Reset le lobby
            lobby.ResetForRematch();

            // Notifier tout le monde que la revanche commence
            await Clients.Group(lobbyId).SendAsync("RematchStarting", new
            {
                message = "Tous les joueurs ont accepté ! La revanche commence..."
            });

            // Attendre un peu pour l'animation
            await Task.Delay(1500);

            // Rediriger vers la salle d'attente (le host peut relancer)
            await Clients.Group(lobbyId).SendAsync("GoToRoom", lobbyId);
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
