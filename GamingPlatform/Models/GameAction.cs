using System;

namespace GamingPlatform.Models
{
    /// <summary>
    /// Représente une action effectuée pendant une partie de jeu
    /// </summary>
    public class GameAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string LobbyId { get; set; } = string.Empty;
        public string GameType { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionDetails { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }
    }

    /// <summary>
    /// Historique complet d'une partie
    /// </summary>
    public class GameHistory
    {
        public string LobbyId { get; set; } = string.Empty;
        public string LobbyName { get; set; } = string.Empty;
        public string GameType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public List<string> Players { get; set; } = new();
        public string? Winner { get; set; }
        public bool IsTie { get; set; }
        public List<GameAction> Actions { get; set; } = new();
    }
}
