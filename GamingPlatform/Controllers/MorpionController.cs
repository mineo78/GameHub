using GamingPlatform.Hubs;
using GamingPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GamingPlatform.Controllers
{
    public class MorpionController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Lobby", new { gameType = "Morpion" });
        }

        [Route("Morpion/Game/{lobbyId}/{playerName}")]
        public IActionResult Game(string lobbyId, string playerName)
        {
            ViewBag.LobbyId = lobbyId;
            ViewBag.PlayerName = playerName;
            return View("Game2");
        }
    }
}

