using Microsoft.AspNetCore.SignalR;
using GamingPlatform.Models.Puissance4;

namespace GamingPlatform.Hubs.Puissance4
{
    public class Puissance4Hub : Hub
    {
        private readonly GameState _gameState;

        public Puissance4Hub(GameState gameState)
        {
            _gameState = gameState;
        }

        public async Task FindGame(string username)
        {
            if (_gameState.IsUsernameTaken(username))
            {
                await Clients.Caller.SendAsync("UsernameTaken");
                return;
            }

            var joiningPlayer = _gameState.CreatePlayer(username, Context.ConnectionId);
            await Clients.Caller.SendAsync("PlayerJoined", joiningPlayer);

            var opponent = _gameState.GetWaitingOpponent();
            if (opponent == null)
            {
                _gameState.AddToWaitingPool(joiningPlayer);
                await Clients.Caller.SendAsync("WaitingForOpponent");
            }
            else
            {
                var newGame = await _gameState.CreateGame(opponent, joiningPlayer);
                await Clients.Group(newGame.Id).SendAsync("GameStart", new
                {
                    gameId = newGame.Id,
                    player1 = new { name = newGame.Player1.Name, color = newGame.Player1.Color },
                    player2 = new { name = newGame.Player2.Name, color = newGame.Player2.Color },
                    board = newGame.Board.Pieces,
                    currentPlayer = newGame.WhoseTurn.Name
                });
            }
        }

        public async Task PlacePiece(int column)
        {
            var playerMakingTurn = _gameState.GetPlayer(Context.ConnectionId);
            if (playerMakingTurn == null)
            {
                await Clients.Caller.SendAsync("Error", "Joueur non trouvé");
                return;
            }

            var game = _gameState.GetGame(playerMakingTurn, out var opponent);
            if (game == null)
            {
                await Clients.Caller.SendAsync("Error", "Partie non trouvée");
                return;
            }

            if (!game.WhoseTurn.Equals(playerMakingTurn))
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

            await Clients.Group(game.Id).SendAsync("PiecePlaced", new
            {
                row = row,
                column = column,
                color = playerMakingTurn.Color,
                player = playerMakingTurn.Name
            });

            if (game.IsOver)
            {
                if (game.IsTie)
                {
                    await Clients.Group(game.Id).SendAsync("GameOver", new
                    {
                        isTie = true,
                        message = "Match nul !"
                    });
                }
                else
                {
                    await Clients.Group(game.Id).SendAsync("GameOver", new
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
                    _gameState.RemoveGame(game.Id);
                });
            }
            else
            {
                await Clients.Group(game.Id).SendAsync("UpdateTurn", new
                {
                    currentPlayer = game.WhoseTurn.Name,
                    currentColor = game.WhoseTurn.Color
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var leavingPlayer = _gameState.GetPlayer(Context.ConnectionId);

            if (leavingPlayer != null)
            {
                if (_gameState.GetGame(leavingPlayer, out var opponent) is { } ongoingGame)
                {
                    await Clients.Group(ongoingGame.Id).SendAsync("OpponentLeft", 
                        $"{leavingPlayer.Name} a quitté la partie");
                    _gameState.RemoveGame(ongoingGame.Id);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
