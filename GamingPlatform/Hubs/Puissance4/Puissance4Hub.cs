using Microsoft.AspNetCore.SignalR;
using GamingPlatform.Models.Puissance4;
using GamingPlatform.Services;

namespace GamingPlatform.Hubs.Puissance4
{
    public class Puissance4Hub : Hub
    {
        private readonly GameState _gameState;
        private readonly LobbyService _lobbyService;

        public Puissance4Hub(GameState gameState, LobbyService lobbyService)
        {
            _gameState = gameState;
            _lobbyService = lobbyService;
        }

        public async Task JoinLobbyGroup(string lobbyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

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

            int row = game.PlacePiece(column);
            if (row == -1)
            {
                await Clients.Caller.SendAsync("Error", "Impossible de placer le jeton");
                return;
            }

            await Clients.Group(lobbyId).SendAsync("PiecePlaced", new
            {
                row = row,
                column = column,
                color = game.WhoseTurn.Color == game.Player1.Color ? game.Player2.Color : game.Player1.Color,
                player = playerName
            });

            if (game.IsOver)
            {
                if (game.IsTie)
                {
                    await Clients.Group(lobbyId).SendAsync("GameOver", new
                    {
                        isTie = true,
                        message = "Match nul !"
                    });
                }
                else
                {
                    await Clients.Group(lobbyId).SendAsync("GameOver", new
                    {
                        isTie = false,
                        winner = game.Winner.Name,
                        winnerColor = game.Winner.Color,
                        message = $"{game.Winner.Name} a gagné !"
                    });
                }

                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    _lobbyService.RemoveLobby(lobbyId);
                });
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


    }
}
