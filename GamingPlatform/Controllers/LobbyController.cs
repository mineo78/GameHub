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
            var result = _lobbyService.JoinLobby(lobbyId, playerName);
            if (result == LobbyService.JoinResult.Success)
            {
                HttpContext.Session.SetString("PlayerName", playerName);
                HttpContext.Session.SetString("LobbyId", lobbyId);
                
                // Notify others
                _hubContext.Clients.Group(lobbyId).SendAsync("PlayerJoined", playerName);
                
                return RedirectToAction("Room", new { id = lobbyId });
            }
            
            var lobby = _lobbyService.GetLobby(lobbyId);
            var gameType = lobby?.GameType ?? "Morpion";
            
            switch (result)
            {
                case LobbyService.JoinResult.NameAlreadyTaken:
                    TempData["Error"] = "Ce nom est déjà utilisé dans ce lobby. Veuillez en choisir un autre.";
                    break;
                case LobbyService.JoinResult.LobbyFull:
                    TempData["Error"] = "Le lobby est complet.";
                    break;
                case LobbyService.JoinResult.GameAlreadyStarted:
                    TempData["Error"] = "La partie a déjà commencé.";
                    break;
                default:
                    TempData["Error"] = "Impossible de rejoindre le lobby.";
                    break;
            }
            
            return RedirectToAction("Index", new { gameType });
        }

        public IActionResult Room(string id)
        {
            var lobby = _lobbyService.GetLobby(id);
            if (lobby == null) return NotFound();

            ViewBag.PlayerName = HttpContext.Session.GetString("PlayerName");
            ViewBag.ShareLink = $"{Request.Scheme}://{Request.Host}/Lobby/JoinByLink/{id}";
            return View(lobby);
        }

        // GET: Affiche le formulaire pour rejoindre via lien public
        [HttpGet]
        public IActionResult JoinByLink(string id)
        {
            var lobby = _lobbyService.GetLobby(id);
            if (lobby == null) return NotFound();

            // Si déjà dans le lobby (session), rediriger vers Room
            var existingPlayer = HttpContext.Session.GetString("PlayerName");
            var existingLobbyId = HttpContext.Session.GetString("LobbyId");
            if (!string.IsNullOrEmpty(existingPlayer) && existingLobbyId == id)
            {
                return RedirectToAction("Room", new { id });
            }

            // Vérifier si le lobby est plein ou déjà commencé
            if (lobby.IsStarted)
            {
                ViewBag.Error = "La partie a déjà commencé.";
                return View("JoinError");
            }
            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                ViewBag.Error = "Le lobby est complet.";
                return View("JoinError");
            }

            return View(lobby);
        }

        // POST: Traite la demande de rejoindre via lien public
        [HttpPost]
        public IActionResult JoinByLink(string id, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return RedirectToAction("JoinByLink", new { id });
            }

            var result = _lobbyService.JoinLobby(id, playerName);
            if (result == LobbyService.JoinResult.Success)
            {
                HttpContext.Session.SetString("PlayerName", playerName);
                HttpContext.Session.SetString("LobbyId", id);
                
                // Notifier les autres joueurs
                _hubContext.Clients.Group(id).SendAsync("PlayerJoined", playerName);
                
                return RedirectToAction("Room", new { id });
            }

            string errorMessage;
            switch (result)
            {
                case LobbyService.JoinResult.NameAlreadyTaken:
                    errorMessage = "Ce nom est déjà utilisé dans ce lobby. Veuillez en choisir un autre.";
                    break;
                case LobbyService.JoinResult.LobbyFull:
                    errorMessage = "Le lobby est complet.";
                    break;
                case LobbyService.JoinResult.GameAlreadyStarted:
                    errorMessage = "La partie a déjà commencé.";
                    break;
                case LobbyService.JoinResult.LobbyNotFound:
                    errorMessage = "Le lobby n'existe pas ou a été supprimé.";
                    break;
                default:
                    errorMessage = "Impossible de rejoindre le lobby.";
                    break;
            }
            
            ViewBag.Error = errorMessage;
            return View("JoinError");
        }
    }
}
