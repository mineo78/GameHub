using GamingPlatform.Models;
using GamingPlatform.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamingPlatform.Hubs
{
	public class SpeedTypingHub : Hub
	{
		private readonly LobbyService _lobbyService;

		public SpeedTypingHub(LobbyService lobbyService)
		{
			_lobbyService = lobbyService;
		}

		public async Task JoinLobbyGroup(string lobbyId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            // Si le jeu est déjà commencé, envoyer l'état actuel
            var lobby = _lobbyService.GetLobby(lobbyId);
            if (lobby != null && lobby.IsStarted && lobby.GameState is SpeedTypingGame game)
            {
                await Clients.Caller.SendAsync("GameStarted", lobby.Players);
                
                // Envoyer aussi la progression actuelle
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
			Console.WriteLine($"StartGame called for lobbyId: {lobbyId}");

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
				// Initialiser le GameState pour SpeedTyping
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

				// Notifier tous les clients du lobby que le jeu a démarré
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

				// Récupérer le GameState
				var speedTypingGame = lobby.GameState as SpeedTypingGame;
				if (speedTypingGame == null)
				{
					throw new InvalidOperationException("GameState invalide.");
				}

				// Mise à jour de la progression
				if (speedTypingGame.Progress.ContainsKey(playerName))
				{
					speedTypingGame.Progress[playerName] = (int)progress;
				}
				else
				{
					speedTypingGame.Progress.Add(playerName, (int)progress);
				}

				// Envoyer la progression aux clients
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

            // Déterminer le podium et le vainqueur
            var podium = speedTypingGame.Progress.OrderByDescending(p => p.Value).Take(3).ToList();
            var winner = podium.FirstOrDefault().Key;

            // Envoyer les données de fin de partie à tous les joueurs du lobby
            await Clients.Group(lobbyId).SendAsync("EndGame", new
            {
                Podium = podium.Select(p => new { Player = p.Key, Progress = p.Value }).ToList(),
                Winner = winner
            });

            // Supprimer le lobby après la fin de la partie
            _lobbyService.RemoveLobby(lobbyId);
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

            // Déterminer le podium et le vainqueur
            var podium = speedTypingGame.Progress.OrderByDescending(p => p.Value).Take(3).ToList();
            var winner = podium.FirstOrDefault().Key;

            // Notifier tous les joueurs de la fin de la partie
            await Clients.Group(lobbyId).SendAsync("EndGame", new
            {
                Podium = podium.Select(p => new { Player = p.Key, Progress = p.Value }).ToList(),
                Winner = winner
            });

            // Supprimer le lobby
            _lobbyService.RemoveLobby(lobbyId);
        }
	}
}

