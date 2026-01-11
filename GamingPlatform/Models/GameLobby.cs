using System;
using System.Collections.Generic;

namespace GamingPlatform.Models
{
    public class GameLobby
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string HostName { get; set; }
        public string GameType { get; set; } // "Morpion", "SpeedTyping", etc.
        public List<string> Players { get; set; } = new List<string>();
        public bool IsStarted { get; set; } = false;
        public int MaxPlayers { get; set; } = 2;
        
        // Generic state storage for the game
        public object GameState { get; set; }

        // Rematch system
        public bool IsGameOver { get; set; } = false;
        public HashSet<string> RematchVotes { get; set; } = new HashSet<string>();
        public HashSet<string> RematchDeclined { get; set; } = new HashSet<string>();

        public void ResetForRematch()
        {
            IsStarted = false;
            IsGameOver = false;
            GameState = null;
            RematchVotes.Clear();
            RematchDeclined.Clear();
        }
    }
}
