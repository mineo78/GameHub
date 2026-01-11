using GamingPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GamingPlatform.Controllers
{
    /// <summary>
    /// Contrôleur d'administration pour consulter l'historique des parties
    /// </summary>

    public class AdminController : Controller
    {
        private readonly GameHistoryService _historyService;
        private const string AdminSessionKey = "IsAdmin";

        public AdminController(GameHistoryService historyService)
        {
            _historyService = historyService;
        }

        // Affiche le formulaire de connexion admin
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == "true")
                return RedirectToAction("GameHistory");
            return View();
        }

        // Traite la connexion admin
        [HttpPost]
        public IActionResult Login(string adminId, string adminPwd)
        {
            if (adminId == "admin" && adminPwd == "admin")
            {
                HttpContext.Session.SetString(AdminSessionKey, "true");
                return RedirectToAction("GameHistory");
            }
            ViewBag.Error = "Identifiant ou mot de passe incorrect.";
            return View();
        }

        // Déconnexion
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(AdminSessionKey);
            return RedirectToAction("Login");
        }

        // Vérifie la session admin pour toutes les actions sauf Login/Logout
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.ActionDescriptor.RouteValues["action"];
            if (action != null && action.ToLower() != "login" && action.ToLower() != "logout")
            {
                if (HttpContext.Session.GetString(AdminSessionKey) != "true")
                {
                    context.Result = RedirectToAction("Login");
                }
            }
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Page principale de l'historique des parties
        /// </summary>
        public IActionResult GameHistory(string? gameType = null, string? playerName = null, string? fromDate = null, string? toDate = null)
        {
            DateTime? from = null;
            DateTime? to = null;

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var parsedFrom))
                from = parsedFrom;

            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var parsedTo))
                to = parsedTo.AddDays(1); // Inclure toute la journée

            var histories = _historyService.SearchHistories(gameType, playerName, from, to);

            ViewBag.GameType = gameType;
            ViewBag.PlayerName = playerName;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(histories);
        }

        /// <summary>
        /// Détail d'une partie spécifique
        /// </summary>
        public IActionResult GameDetails(string id)
        {
            var history = _historyService.GetHistory(id);
            if (history == null)
            {
                return NotFound();
            }
            return View(history);
        }

        /// <summary>
        /// Export JSON d'une partie
        /// </summary>
        public IActionResult ExportJson(string id)
        {
            var json = _historyService.ExportHistoryJson(id);
            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"game_history_{id}.json");
        }

        /// <summary>
        /// Export de toutes les parties en JSON
        /// </summary>
        public IActionResult ExportAllJson()
        {
            var histories = _historyService.GetAllHistories();
            var json = System.Text.Json.JsonSerializer.Serialize(histories, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"all_game_histories_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }
    }
}
