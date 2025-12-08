using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace GamingPlatform.Views.SpeedTyping
{
    public class GameModel : PageModel
    {
        // Propriétés pour stocker les données du jeu
        public string TextToType { get; private set; }
        public int Score { get; private set; }
        public string UserInput { get; set; }

        // Méthode appelée lors d'une requête GET
        public void OnGet()
        {
            // Initialiser les données du jeu
            TextToType = "Le texte à taper pour le jeu de dactylographie rapide.";
            Score = 0;
            UserInput = string.Empty;
        }

        // Méthode pour mettre à jour le score
        public void UpdateScore(int points)
        {
            Score += points;
        }

        // Méthode pour vérifier l'entrée de l'utilisateur
        public bool CheckUserInput()
        {
            return UserInput.Equals(TextToType, StringComparison.OrdinalIgnoreCase);
        }

        // Méthode pour réinitialiser le jeu
        public void ResetGame()
        {
            TextToType = "Le texte à taper pour le jeu de dactylographie rapide.";
            Score = 0;
            UserInput = string.Empty;
        }
    }
}
