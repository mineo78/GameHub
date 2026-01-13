using System;
using System.Collections.Generic;
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
        private readonly GameHistoryService _historyService;

        public MorpionHub(GameState gameState, LobbyService lobbyService, GameHistoryService historyService)
        {
            _gameState = gameState;
            _lobbyService = lobbyService;
            _historyService = historyService;
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
            var existingGame = _gameState.GetGame(lobbyId);
            
            if (existingGame != null)
            {
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
                _historyService.StartGame(lobbyId, lobby.Name, "Morpion", new List<string> { opponent.Name, player.Name });
                _historyService.LogAction(lobbyId, "Morpion", "Système", "GameStart", $"Partie démarrée: {opponent.Name} (X) vs {player.Name} (O)");
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

            _historyService.LogAction(game.Id, "Morpion", playerMakingTurn.Name, "PlacePiece", 
                $"Pièce {playerMakingTurn.Piece} placée en ({row}, {col})",
                new Dictionary<string, object> { { "row", row }, { "col", col }, { "piece", playerMakingTurn.Piece } });

            await Clients.Group(game.Id).SendAsync("ActionLogged", new
            {
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                playerName = playerMakingTurn.Name,
                actionType = "PlacePiece",
                description = $"Case ({row + 1}, {col + 1})",
                piece = playerMakingTurn.Piece
            });

            await Clients.Group(game.Id).SendAsync("piecePlaced", row, col, playerMakingTurn.Piece);

            if (!game.IsOver)
            {
                await Clients.Group(game.Id).SendAsync("updateTurn", game);
            }
            else
            {
                if (game.IsTie)
                {
                    _historyService.EndGame(game.Id, null, true);
                    await Clients.Group(game.Id).SendAsync("tieGame");
                }
                else
                {
                    _historyService.EndGame(game.Id, playerMakingTurn.Name, false);
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
                if (_gameState.GetGame(leavingPlayer, out var opponent) is { } ongoingGame)
                {
                    _historyService.LogAction(ongoingGame.Id, "Morpion", leavingPlayer.Name, "Disconnect", 
                        $"Joueur {leavingPlayer.Name} s'est déconnecté");
                    _historyService.EndGame(ongoingGame.Id, opponent?.Name, false);
                    
                    await Clients.Group(ongoingGame.Id).SendAsync("opponentLeft");
                    _lobbyService.RemoveLobby(ongoingGame.Id);
                    _gameState.RemoveGame(ongoingGame.Id);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
