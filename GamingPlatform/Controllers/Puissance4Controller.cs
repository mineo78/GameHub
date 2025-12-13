using Microsoft.AspNetCore.Mvc;
using GamingPlatform.Services;

namespace GamingPlatform.Controllers
{
    public class Puissance4Controller : Controller
    {
        private readonly LobbyService _lobbyService;

        public Puissance4Controller(LobbyService lobbyService)
        {
            _lobbyService = lobbyService;
        }

        // Page d'accueil - Liste des lobbies Puissance4
        public IActionResult Index()
        {
            var lobbies = _lobbyService.GetLobbies("Puissance4");
            return View(lobbies);
        }

        // Afficher la page de jeu
        public IActionResult Game(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

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
            ViewBag.Lobby = lobby;

            return View();
        }
    }
}
