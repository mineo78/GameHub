using Microsoft.AspNetCore.SignalR;
using GamingPlatform.Models.Puissance4;
using GamingPlatform.Services;
using System.Collections.Concurrent;

namespace GamingPlatform.Hubs.Puissance4
{
    public class Puissance4Hub : Hub
    {
        private readonly GameState _gameState;
        private readonly LobbyService _lobbyService;
        private readonly GameHistoryService _historyService;
        
        // Mapping ConnectionId -> (LobbyId, PlayerName)
        private static readonly ConcurrentDictionary<string, (string lobbyId, string playerName)> _playerConnections = new();

        public Puissance4Hub(GameState gameState, LobbyService lobbyService, GameHistoryService historyService)
        {
            _gameState = gameState;
            _lobbyService = lobbyService;
            _historyService = historyService;
        }

        public async Task JoinLobbyGroup(string lobbyId, string playerName = null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            
            // Enregistrer la connexion du joueur
            if (!string.IsNullOrEmpty(playerName))
            {
                _playerConnections[Context.ConnectionId] = (lobbyId, playerName);
            }

            // Si le jeu est déjà commencé, envoyer l'état actuel au joueur qui rejoint
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby != null && lobby.IsStarted && lobby.GameState is Game game)
            {
                await Clients.Caller.SendAsync("GameStart", new
                {
                    gameId = game.Id,
                    player1 = new { name = game.Player1.Name, color = game.Player1.Color },
                    player2 = new { name = game.Player2.Name, color = game.Player2.Color },
                    board = game.Board.Pieces,
                    currentPlayer = game.WhoseTurn.Name
                });
            }
        }

        public void RegisterPlayer(string lobbyId, string playerName)
        {
            _playerConnections[Context.ConnectionId] = (lobbyId, playerName);
        }

        public async Task LeaveLobbyGroup(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        public async Task StartGame(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("Error", "Lobby introuvable.");
                return;
            }

            if (lobby.Players.Count < 2)
            {
                await Clients.Caller.SendAsync("Error", "Il faut au moins 2 joueurs pour commencer.");
                return;
            }

            // Créer les joueurs
            var player1 = new Player(lobby.Players[0], lobby.Players[0]);
            var player2 = new Player(lobby.Players[1], lobby.Players[1]);

            // Créer le jeu
            var newGame = new Game(player1, player2);
            
            // Stocker le jeu dans le GameState du lobby
            lobby.GameState = newGame;
            _lobbyService.StartGame(lobbyId);

            // Logger le démarrage de la partie
            _historyService.StartGame(lobbyId, lobby.Name, "Puissance4", lobby.Players.ToList());

            // Notifier tous les clients
            await Clients.Group(lobbyId).SendAsync("GameStart", new
            {
                gameId = newGame.Id,
                player1 = new { name = newGame.Player1.Name, color = newGame.Player1.Color },
                player2 = new { name = newGame.Player2.Name, color = newGame.Player2.Color },
                board = newGame.Board.Pieces,
                currentPlayer = newGame.WhoseTurn.Name
            });
        }

        public async Task PlacePiece(string lobbyId, string playerName, int column)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("Error", "Lobby introuvable");
                return;
            }

            var game = lobby.GameState as Game;
            if (game == null)
            {
                await Clients.Caller.SendAsync("Error", "Partie non trouvée");
                return;
            }

            // Vérifier que c'est le tour du joueur
            if (game.WhoseTurn.Name != playerName)
            {
                await Clients.Caller.SendAsync("NotYourTurn", "Ce n'est pas votre tour");
                return;
            }

            if (!game.IsValidMove(column))
            {
                await Clients.Caller.SendAsync("InvalidMove", "Mouvement invalide (colonne pleine)");
                return;
            }

            var playerColor = game.WhoseTurn.Color;

            int row = game.PlacePiece(column);
            if (row == -1)
            {
                await Clients.Caller.SendAsync("Error", "Impossible de placer le jeton");
                return;
            }

            // Logger l'action
            _historyService.LogAction(lobbyId, "Puissance4", playerName, "PLACE_PIECE", 
                $"Jeton placé en colonne {column + 1}, ligne {row + 1}", new { row, column, color = playerColor });

            // Envoyer l'action à tous les clients pour l'historique en temps réel
            await Clients.Group(lobbyId).SendAsync("ActionLogged", new
            {
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                playerName = playerName,
                actionType = "PLACE_PIECE",
                description = $"Colonne {column + 1}",
                color = playerColor
            });

            await Clients.Group(lobbyId).SendAsync("PiecePlaced", new
            {
                row = row,
                column = column,
                color = playerColor,
                player = playerName
            });

            if (game.IsOver)
            {
                if (game.IsTie)
                {
                    _historyService.EndGame(lobbyId, null, true);
                    await Clients.Group(lobbyId).SendAsync("GameOver", new
                    {
                        isTie = true,
                        message = "Match nul !"
                    });
                }
                else
                {
                    _historyService.EndGame(lobbyId, game.Winner.Name, false);
                    await Clients.Group(lobbyId).SendAsync("GameOver", new
                    {
                        isTie = false,
                        winner = game.Winner.Name,
                        winnerColor = game.Winner.Color,
                        message = $"{game.Winner.Name} a gagné !"
                    });
                }

                // Marquer la partie comme terminée (ne pas supprimer le lobby pour permettre le rejeu)
                lobby.IsGameOver = true;
            }
            else
            {
                await Clients.Group(lobbyId).SendAsync("UpdateTurn", new
                {
                    currentPlayer = game.WhoseTurn.Name,
                    currentColor = game.WhoseTurn.Color
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_playerConnections.TryRemove(Context.ConnectionId, out var playerInfo))
            {
                var lobby = _lobbyService.GetLobby(playerInfo.lobbyId);
                if (lobby != null && lobby.IsStarted && !lobby.IsGameOver)
                {
                    // La partie est en cours, notifier les autres joueurs
                    await Clients.Group(playerInfo.lobbyId).SendAsync("OpponentLeft", new
                    {
                        playerName = playerInfo.playerName,
                        message = $"{playerInfo.playerName} a quitté la partie."
                    });
                    
                    // Supprimer le jeu et le lobby pour éviter les états incohérents
                    try { _gameState.RemoveGame(playerInfo.lobbyId); } catch { }
                    _lobbyService.RemoveLobby(playerInfo.lobbyId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
