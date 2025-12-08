using GamingPlatform.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GamingPlatform.Hubs
{
	public class SpeedTypingHub : Hub
	{
		public async Task PlayerJoined(string lobbyId, string playerName)
		{
			// Notifier tous les clients du lobby qu'un joueur a rejoint
			await Clients.Group(lobbyId).SendAsync("PlayerJoined", playerName);
		}

		public virtual async Task StartGame(string lobbyId)
		{

			Console.WriteLine($"StartGame called for lobbyId: {lobbyId}");

			if (string.IsNullOrEmpty(lobbyId))
			{
				await Clients.Caller.SendAsync("Error", "L'identifiant du lobby ne peut pas être null ou vide.");
				return;
			}

			if (!SpeedTypingController.lobbies.ContainsKey(lobbyId))
			{
				await Clients.Caller.SendAsync("Error", "Lobby introuvable.");
				return;
			}

			var lobby = SpeedTypingController.lobbies[lobbyId];

			try
			{
				string errorMessage = lobby.StartGame();

				if (!string.IsNullOrEmpty(errorMessage))
				{
					await Clients.Caller.SendAsync("Error", errorMessage);
					return;
				}

				// Notifier tous les clients du lobby que le jeu a démarré
				await Clients.Group(lobbyId).SendAsync("GameStarted", lobby.Players);
			}
			catch (Exception ex)
			{
				await Clients.Caller.SendAsync("Error", $"Erreur lors du démarrage du jeu : {ex.Message}");
			}
		}

		public override Task OnConnectedAsync()
		{
			return base.OnConnectedAsync();
		}

		public override Task OnDisconnectedAsync(Exception? exception)
		{
			return base.OnDisconnectedAsync(exception);
		}

		public virtual async Task JoinLobby(string id, string player)
		{
			// Validation des paramètres
			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(player))
			{
				await Clients.Caller.SendAsync("Error", "L'identifiant du lobby ou le nom du joueur ne peut pas être vide.");
				return;
			}

			if (!SpeedTypingController.lobbies.ContainsKey(id))
			{
				await Clients.Caller.SendAsync("Error", "Lobby introuvable.");
				return;
			}

			var lobby = SpeedTypingController.lobbies[id];

			// Toujours ajouter la connexion au groupe SignalR pour recevoir les mises à jour
			await Groups.AddToGroupAsync(Context.ConnectionId, id);

			// Vérifier si le joueur existe déjà dans le lobby
			if (!lobby.Players.Contains(player))
			{
				lobby.AddPlayer(player);
				// Notifier les autres seulement si c'est un nouveau joueur
				await this.Clients.Group(id).SendAsync("PlayerJoined", player);
			}
		}		public async Task UpdatePlayerProgress(string lobbyId, string playerName, double progress)
		{
			try
			{
				if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(playerName))
				{
					throw new ArgumentException("Lobby ID et Player Name ne peuvent pas être vides.");
				}

				if (!SpeedTypingController.lobbies.ContainsKey(lobbyId))
				{
					throw new KeyNotFoundException("Lobby introuvable.");
				}

				var lobby = SpeedTypingController.lobbies[lobbyId];

				if (!lobby.Players.Contains(playerName))
				{
					throw new InvalidOperationException("Le joueur n'existe pas dans ce lobby.");
				}

				// Mise à jour de la progression
				if (lobby.Progress.ContainsKey(playerName))
				{
					lobby.Progress[playerName] = (int)progress;
				}
				else
				{
					lobby.Progress.Add(playerName, (int)progress);
				}

				// Envoyer la progression aux clients
				await Clients.Group(lobbyId).SendAsync("PlayerProgressUpdated", playerName, progress);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Erreur dans UpdatePlayerProgress : {ex.Message}");
				throw; // Signale l'erreur au client
			}
		}

        public async Task GameOver(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId) || !SpeedTypingController.lobbies.ContainsKey(lobbyId))
            {
                throw new KeyNotFoundException("Lobby introuvable.");
            }

            var lobby = SpeedTypingController.lobbies[lobbyId];

            // Déterminer le podium et le vainqueur
            var podium = lobby.Progress.OrderByDescending(p => p.Value).Take(3).ToList();
            var winner = podium.FirstOrDefault().Key;

            // Envoyer les données de fin de partie à tous les joueurs du lobby
            await Clients.Group(lobbyId).SendAsync("EndGame", new
            {
                Podium = podium.Select(p => new { Player = p.Key, Progress = p.Value }).ToList(),
                Winner = winner
            });

            // Supprimez le lobby après la fin de la partie si nécessaire
            SpeedTypingController.lobbies.Remove(lobbyId);
        }

        public async Task NotifyGameOver(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId) || !SpeedTypingController.lobbies.ContainsKey(lobbyId))
            {
                throw new KeyNotFoundException("Lobby introuvable.");
            }

            var lobby = SpeedTypingController.lobbies[lobbyId];

            // Déterminer le podium et le vainqueur
            var podium = lobby.Progress.OrderByDescending(p => p.Value).Take(3).ToList();
            var winner = podium.FirstOrDefault().Key;

            // Notifier tous les joueurs de la fin de la partie
            await this.Clients.Group(lobbyId).SendAsync("EndGame", new
            {
                Podium = podium.Select(p => new { Player = p.Key, Progress = p.Value }).ToList(),
                Winner = winner
            });

            // Supprimer le lobby si nécessaire
            SpeedTypingController.lobbies.Remove(lobbyId);

        }





        /*	public IActionResult JoinLobby(string id, string player)
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(player))
                {
                    TempData["Message"] = "L'identifiant du lobby ou le nom du joueur ne peut pas être vide.";
                    return RedirectToAction("Lobby", new { id });
                }

                if (!lobbies.ContainsKey(id))
                {
                    TempData["Message"] = "Lobby introuvable.";
                    return RedirectToAction("Index");
                }

                var lobby = lobbies[id];

                if (lobby.Players.Contains(player))
                {
                    TempData["Message"] = "Un joueur avec ce nom existe déjà dans ce lobby.";
                    return RedirectToAction("Lobby", new { id });
                }

                lobby.AddPlayer(player);

                HttpContext.Session.SetString("CurrentPlayer", player);
                HttpContext.Session.SetString("LobbyId", id);

                _hubContext.Clients.Group(id).SendAsync("PlayerJoined", player);
                return RedirectToAction("Lobby", new { id });
            }
        */
        // Classe de simulation d'un lobby



        public class Lobby
		{
			public string Id { get; set; }
			public List<string> Players { get; set; } = new();

			public string StartGame()
			{
				// Simulation de logique de démarrage de jeu
				if (Players.Count == 0)
				{
					return "Le lobby est vide, impossible de démarrer le jeu.";
				}

				Console.WriteLine("Le jeu démarre avec les joueurs : " + string.Join(", ", Players));
				return string.Empty; // Pas d'erreur
			}
		}
	}
}

