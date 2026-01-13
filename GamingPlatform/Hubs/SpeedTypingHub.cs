using GamingPlatform.Models;
using GamingPlatform.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamingPlatform.Hubs
{
    public class SpeedTypingHub : Hub
    {
        private readonly LobbyService _lobbyService;
        private readonly GameHistoryService _historyService;
        private static readonly ConcurrentDictionary<string, (string lobbyId, string playerName)> _playerConnections = new();

        public SpeedTypingHub(LobbyService lobbyService, GameHistoryService historyService)
        {
            _lobbyService = lobbyService;
            _historyService = historyService;
        }

        public async Task JoinLobbyGroup(string lobbyId, string playerName = null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            
            if (!string.IsNullOrEmpty(playerName))
            {
                _playerConnections[Context.ConnectionId] = (lobbyId, playerName);
            }

            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby != null && lobby.IsStarted && lobby.GameState is SpeedTypingGame game)
            {
                await Clients.Caller.SendAsync("GameStarted", lobby.Players);
                
                foreach(var player in game.Progress)
                {
                    await Clients.Caller.SendAsync("PlayerProgressUpdated", player.Key, player.Value);
                }
            }
        }

        public async Task LeaveLobbyGroup(string lobbyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
        }

        public async Task StartGame(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId))
            {
                await Clients.Caller.SendAsync("Error", "L'identifiant du lobby ne peut pas être null ou vide.");
                return;
            }

            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("Error", "Lobby introuvable.");
                return;
            }

            try
            {
                if (lobby.GameState == null)
                {
                    var speedTypingGame = new SpeedTypingGame(lobby.HostName);
                    foreach (var player in lobby.Players)
                    {
                        speedTypingGame.Players.Add(player);
                    }
                    lobby.GameState = speedTypingGame;
                }

                var speedTypingGame2 = (SpeedTypingGame)lobby.GameState;
                string errorMessage = speedTypingGame2.StartGame();

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    await Clients.Caller.SendAsync("Error", errorMessage);
                    return;
                }

                _lobbyService.StartGame(lobbyId);
                _historyService.StartGame(lobbyId, lobby.Name, "SpeedTyping", lobby.Players.ToList());
                _historyService.LogAction(lobbyId, "SpeedTyping", "Système", "GameStart", 
                    $"Partie SpeedTyping démarrée avec {lobby.Players.Count} joueurs");

                await Clients.Group(lobbyId).SendAsync("GameStarted", lobby.Players);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Erreur lors du démarrage du jeu : {ex.Message}");
            }
        }

        public async Task UpdatePlayerProgress(string lobbyId, string playerName, double progress)
        {
            try
            {
                if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(playerName))
                {
                    throw new ArgumentException("Lobby ID et Player Name ne peuvent pas être vides.");
                }

                var lobby = _lobbyService.GetLobby(lobbyId);
                if (lobby == null)
                {
                    throw new KeyNotFoundException("Lobby introuvable.");
                }

                if (!lobby.Players.Contains(playerName))
                {
                    throw new InvalidOperationException("Le joueur n'existe pas dans ce lobby.");
                }

                var speedTypingGame = lobby.GameState as SpeedTypingGame;
                if (speedTypingGame == null)
                {
                    throw new InvalidOperationException("GameState invalide.");
                }

                if (speedTypingGame.Progress.ContainsKey(playerName))
                {
                    speedTypingGame.Progress[playerName] = (int)progress;
                }
                else
                {
                    speedTypingGame.Progress.Add(playerName, (int)progress);
                }

                if ((int)progress % 25 == 0 || progress >= 100)
                {
                    _historyService.LogAction(lobbyId, "SpeedTyping", playerName, "Progress", 
                        $"Progression: {(int)progress}%",
                        new Dictionary<string, object> { { "progress", progress } });
                }

                await Clients.Group(lobbyId).SendAsync("PlayerProgressUpdated", playerName, progress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans UpdatePlayerProgress : {ex.Message}");
                throw;
            }
        }

        public async Task GameOver(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null)
            {
                throw new KeyNotFoundException("Lobby introuvable.");
            }

            var speedTypingGame = lobby.GameState as SpeedTypingGame;
            if (speedTypingGame == null)
            {
                throw new InvalidOperationException("GameState invalide.");
            }

            var allPlayersProgress = lobby.Players.Select(p => new
            {
                Player = p,
                Progress = speedTypingGame.Progress.ContainsKey(p) ? speedTypingGame.Progress[p] : 0
            }).ToList();

            var podium = allPlayersProgress.OrderByDescending(p => p.Progress).ToList();
            var winner = podium.FirstOrDefault()?.Player;

            await Clients.Group(lobbyId).SendAsync("EndGame", new
            {
                Podium = podium,
                Winner = winner
            });

            _historyService.LogAction(lobbyId, "SpeedTyping", "Système", "GameEnd", 
                $"Partie terminée. Vainqueur: {winner ?? "Aucun"}",
                new Dictionary<string, object> { 
                    { "podium", podium.Select(p => new { p.Player, p.Progress }).ToList() } 
                });
            _historyService.EndGame(lobbyId, winner, false);

            lobby.IsGameOver = true;
        }

        public async Task NotifyGameOver(string lobbyId)
        {
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby == null)
            {
                throw new KeyNotFoundException("Lobby introuvable.");
            }

            var speedTypingGame = lobby.GameState as SpeedTypingGame;
            if (speedTypingGame == null)
            {
                throw new InvalidOperationException("GameState invalide.");
            }

            var allPlayersProgress = lobby.Players.Select(p => new
            {
                Player = p,
                Progress = speedTypingGame.Progress.ContainsKey(p) ? speedTypingGame.Progress[p] : 0
            }).ToList();

            var podium = allPlayersProgress.OrderByDescending(p => p.Progress).ToList();
            var winner = podium.FirstOrDefault()?.Player;

            await Clients.Group(lobbyId).SendAsync("EndGame", new
            {
                Podium = podium,
                Winner = winner
            });

            lobby.IsGameOver = true;
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_playerConnections.TryRemove(Context.ConnectionId, out var playerInfo))
            {
                var lobby = _lobbyService.GetLobby(playerInfo.lobbyId);
                if (lobby != null && lobby.IsStarted && !lobby.IsGameOver)
                {
                    _historyService.LogAction(playerInfo.lobbyId, "SpeedTyping", playerInfo.playerName, "Disconnect", 
                        $"Joueur {playerInfo.playerName} a quitté la partie");
                    _historyService.EndGame(playerInfo.lobbyId, null, false);
                    
                    await Clients.Group(playerInfo.lobbyId).SendAsync("PlayerLeft", new
                    {
                        playerName = playerInfo.playerName,
                        message = $"{playerInfo.playerName} a quitté la partie."
                    });
                    
                    _lobbyService.RemoveLobby(playerInfo.lobbyId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
