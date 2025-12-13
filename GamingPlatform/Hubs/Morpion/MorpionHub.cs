using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using GamingPlatform.Models;
using GamingPlatform.Services;

namespace GamingPlatform.Hubs.Morpion
{
    public class MorpionHub : Hub
    {
        private readonly GameState _gameState;
        private readonly LobbyService _lobbyService;

        public MorpionHub(GameState gameState, LobbyService lobbyService)
        {
            _gameState = gameState;
            _lobbyService = lobbyService;
        }

        public async Task JoinGame(string lobbyId, string username)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("error", "Lobby not found");
                return;
            }

            var player = _gameState.CreatePlayer(username, Context.ConnectionId);
            
            // Check if game already exists (reconnection?)
            var existingGame = _gameState.GetGame(lobbyId);
            if (existingGame != null)
            {
                // Reconnection logic
                bool isReconnection = false;
                if (existingGame.Player1.Name == username)
                {
                    existingGame.Player1 = player;
                    player.GameId = existingGame.Id;
                    player.Piece = "X"; 
                    isReconnection = true;
                }
                else if (existingGame.Player2.Name == username)
                {
                    existingGame.Player2 = player;
                    player.GameId = existingGame.Id;
                    player.Piece = "O";
                    isReconnection = true;
                }

                if (isReconnection)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, existingGame.Id);
                    await Clients.Caller.SendAsync("start", existingGame);
                }
                else 
                {
                     await Clients.Caller.SendAsync("error", "Game already in progress");
                }
                return;
            }

            var opponent = _gameState.RegisterLobbyPlayer(lobbyId, player);
            if (opponent != null)
            {
                var newGame = await _gameState.CreateGame(opponent, player, lobbyId);
                await Clients.Group(newGame.Id).SendAsync("start", newGame);
            }
            else
            {
                await Clients.Caller.SendAsync("waitingForOpponent");
            }
        }

        public async Task PlacePiece(int row, int col)
        {
            var playerMakingTurn = _gameState.GetPlayer(Context.ConnectionId);
            if (playerMakingTurn == null)
            {
                await Clients.Caller.SendAsync("notPlayersTurn");
                return;
            }

            if (!_gameState.GetGame(playerMakingTurn, out var opponent)?.WhoseTurn.Equals(playerMakingTurn) ?? true)
            {
                await Clients.Caller.SendAsync("notPlayersTurn");
                return;
            }

            if (!_gameState.GetGame(playerMakingTurn, out opponent)?.IsValidMove(row, col) ?? false)
            {
                await Clients.Caller.SendAsync("notValidMove");
                return;
            }

            var game = _gameState.GetGame(playerMakingTurn, out opponent);
            game.PlacePiece(row, col);

            await Clients.Group(game.Id).SendAsync("piecePlaced", row, col, playerMakingTurn.Piece);

            if (!game.IsOver)
            {
                await Clients.Group(game.Id).SendAsync("updateTurn", game);
            }
            else
            {
                if (game.IsTie)
                {
                    await Clients.Group(game.Id).SendAsync("tieGame");
                }
                else
                {
                    await Clients.Group(game.Id).SendAsync("winner", playerMakingTurn.Name);
                }

                _gameState.RemoveGame(game.Id);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var leavingPlayer = _gameState.GetPlayer(Context.ConnectionId);

            if (leavingPlayer != null)
            {
                Console.WriteLine($"Player disconnected: {leavingPlayer.Id}");

       
                if (_gameState.GetGame(leavingPlayer, out var opponent) is { } ongoingGame)
                {
                    Console.WriteLine($"Notifying opponent of player {leavingPlayer.Id} leaving.");
                    await Clients.Group(ongoingGame.Id).SendAsync("opponentLeft");

                    _gameState.RemoveGame(ongoingGame.Id);
                }
                else
                {
                    Console.WriteLine($"Player {leavingPlayer.Id} is not in any game.");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

    }
}
