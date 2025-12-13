using GamingPlatform.Services;
using GamingPlatform.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;

namespace GamingPlatform.Controllers
{
    public class LobbyController : Controller
    {
        private readonly LobbyService _lobbyService;
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyController(LobbyService lobbyService, IHubContext<LobbyHub> hubContext)
        {
            _lobbyService = lobbyService;
            _hubContext = hubContext;
        }

        public IActionResult Index(string gameType)
        {
            ViewBag.GameType = gameType;
            var lobbies = _lobbyService.GetLobbies(gameType);
            return View(lobbies);
        }

        [HttpPost]
        public IActionResult Create(string gameType, string name, string playerName)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(playerName))
            {
                return RedirectToAction("Index", new { gameType });
            }

            // SpeedTyping peut avoir plus de joueurs
            int maxPlayers = gameType == "SpeedTyping" ? 10 : 2;
            var lobby = _lobbyService.CreateLobby(name, playerName, gameType, maxPlayers);
            
            // Set session
            HttpContext.Session.SetString("PlayerName", playerName);
            HttpContext.Session.SetString("LobbyId", lobby.Id);

            return RedirectToAction("Room", new { id = lobby.Id });
        }

        [HttpPost]
        public IActionResult Join(string lobbyId, string playerName)
        {
            if (_lobbyService.JoinLobby(lobbyId, playerName))
            {
                HttpContext.Session.SetString("PlayerName", playerName);
                HttpContext.Session.SetString("LobbyId", lobbyId);
                
                // Notify others
                _hubContext.Clients.Group(lobbyId).SendAsync("PlayerJoined", playerName);
                
                return RedirectToAction("Room", new { id = lobbyId });
            }
            
            return RedirectToAction("Index", new { gameType = _lobbyService.GetLobby(lobbyId)?.GameType ?? "Morpion" });
        }

        public IActionResult Room(string id)
        {
            var lobby = _lobbyService.GetLobby(id);
            if (lobby == null) return NotFound();

            ViewBag.PlayerName = HttpContext.Session.GetString("PlayerName");
            return View(lobby);
        }
    }
}
