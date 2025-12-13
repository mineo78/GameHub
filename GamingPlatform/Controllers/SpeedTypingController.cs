using Microsoft.AspNetCore.Mvc;
using GamingPlatform.Models;
using GamingPlatform.Services;
using GamingPlatform.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GamingPlatform.Controllers
{
    public class SpeedTypingController : Controller
    {
        private readonly LobbyService _lobbyService;
        private readonly IHubContext<SpeedTypingHub> _hubContext;

        public SpeedTypingController(LobbyService lobbyService, IHubContext<SpeedTypingHub> hubContext)
        {
            _lobbyService = lobbyService;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            var lobbies = _lobbyService.GetLobbies("SpeedTyping");
            return View(lobbies);
        }



        public IActionResult Game(string id)
        {
            var lobby = _lobbyService.GetLobby(id);
            if (lobby == null)
            {
                return NotFound("Lobby introuvable.");
            }

            if (!lobby.IsStarted)
            {
                TempData["Message"] = "Le jeu n'a pas encore démarré.";
                return RedirectToAction("Index");
            }

            var currentPlayer = HttpContext.Session.GetString("PlayerName");
            if (string.IsNullOrEmpty(currentPlayer) || !lobby.Players.Contains(currentPlayer))
            {
                TempData["Message"] = "Vous n'êtes pas autorisé à accéder à ce jeu.";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentPlayer = currentPlayer;
            ViewBag.LobbyId = id;

            // Initialiser GameState si nécessaire
            if (lobby.GameState == null)
            {
                lobby.GameState = new SpeedTypingGame(lobby.HostName);
                foreach (var player in lobby.Players)
                {
                    ((SpeedTypingGame)lobby.GameState).Players.Add(player);
                }
            }

            return View(lobby.GameState);
        }

        public IActionResult GameOver(string id)
        {
            return View("GameOver");
        }
    }
}
