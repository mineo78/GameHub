using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using GamePlateforme.Models;
using GamingPlatform.Hubs;

namespace GamingPlatform.Controllers
{
    public class Puissance4Controller : Controller
    {
        private readonly ILogger<Puissance4Controller> _logger;

        public Puissance4Controller(ILogger<Puissance4Controller> logger)
        {
            _logger = logger;
        }

        // Page d'accueil (accueil des joueurs pour la création et la jonction de lobbys)
        public IActionResult Index()
        {
            return View();
        }

        // Afficher la page de jeu
        public IActionResult Game()
        {
            return View();
        }

        // Afficher le lobby de jeu (legacy - peut être supprimé si non utilisé)
        public IActionResult Lobby(string lobbyName)
        {
            if (string.IsNullOrEmpty(lobbyName))
            {
                return RedirectToAction("Index");
            }

            // Passer les informations du lobby à la vue
            ViewBag.LobbyName = lobbyName;
            return View();
        }

        // Accéder au jeu (legacy - peut être supprimé si non utilisé)
        public IActionResult Puissance4(string lobby, string player)
        {
            ViewBag.LobbyName = lobby;
            ViewBag.PlayerName = player;
            return View();
        }

        // Créer un lobby
        [HttpPost]
        public IActionResult CreateLobby(string lobbyName, string playerName)
        {
            if (string.IsNullOrEmpty(lobbyName) || string.IsNullOrEmpty(playerName))
            {
                return RedirectToAction("Index");
            }

            // Créer un nouveau lobby et rediriger vers le lobby
            return RedirectToAction("Lobby", new { lobbyName });
        }

        // Rejoindre un lobby
        [HttpPost]
public IActionResult JoinLobby(string lobbyName, string playerName)
{
    if (string.IsNullOrEmpty(lobbyName) || string.IsNullOrEmpty(playerName))
    {
        return RedirectToAction("Index");
    }

    // Vérifiez si le lobby existe
    // Redirigez vers la page de jeu avec les bons paramètres
    return RedirectToAction("Puissance4", new { lobby = lobbyName, player = playerName });
}


        // Page de confidentialité
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
