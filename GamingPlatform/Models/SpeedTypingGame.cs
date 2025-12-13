using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace GamingPlatform.Models
{
    public class SpeedTypingGame
    {
        // Hôte du jeu
        public string Host { get; set; }

        // Liste des joueurs
        public List<string> Players { get; private set; } = new List<string>();

        // Progression des joueurs (en pourcentage ou points)
        public Dictionary<string, int> Progress { get; private set; } = new Dictionary<string, int>();

        // Indicateur si le jeu a commencé
        public bool IsStarted { get; private set; } = false;

        // Texte à taper pour le jeu
        public string TextToType { get; private set; } = "NOT STARTED";

        private static readonly List<string> FrenchWords = new List<string>
        {
            "maison", "chat", "chien", "voiture", "arbre", "soleil", "lune", "étoile", "mer", "montagne",
            "rivière", "forêt", "fleur", "oiseau", "poisson", "ciel", "nuage", "pluie", "neige", "vent",
            "feu", "terre", "eau", "air", "vie", "amour", "joie", "paix", "liberté", "espoir",
            "rêve", "voyage", "musique", "art", "livre", "école", "travail", "argent", "temps", "heure",
            "jour", "nuit", "semaine", "mois", "année", "siècle", "histoire", "monde", "pays", "ville",
            "rue", "chemin", "porte", "fenêtre", "table", "chaise", "lit", "chambre", "cuisine", "salle",
            "jardin", "parc", "place", "marché", "magasin", "restaurant", "café", "hôtel", "banque", "poste",
            "lettre", "message", "téléphone", "ordinateur", "internet", "réseau", "ami", "famille", "enfant", "parent",
            "père", "mère", "frère", "soeur", "oncle", "tante", "cousin", "grand-père", "grand-mère", "voisin",
            "homme", "femme", "garçon", "fille", "bébé", "adulte", "vieux", "jeune", "grand", "petit",
            "gros", "mince", "beau", "joli", "laid", "fort", "faible", "rapide", "lent", "intelligent",
            "rouge", "bleu", "vert", "jaune", "noir", "blanc", "gris", "orange", "violet", "rose",
            "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix",
            "manger", "boire", "dormir", "courir", "marcher", "parler", "écouter", "voir", "regarder", "sentir",
            "aimer", "détester", "vouloir", "pouvoir", "devoir", "savoir", "connaître", "penser", "croire", "comprendre"
        };

        // Constructeur
        public SpeedTypingGame(string host)
        {
            Host = host;
        }

        // Ajouter un joueur au jeu
        public void AddPlayer(string player)
        {
            if (!IsStarted && !Players.Contains(player))
            {
                Players.Add(player);
                Progress[player] = 0;
            }
        }

        // Mettre à jour la progression d'un joueur
        public void UpdateProgress(string player, int progress)
        {
            if (Players.Contains(player) && IsStarted)
            {
                Progress[player] = Math.Clamp(progress, 0, 100); // S'assurer que la progression est entre 0 et 100
            }
        }

        // Commencer le jeu
        public string StartGame()
        {
            if (Players.Count < 2)
            {
                return "Il faut un minimum de 2 joueurs pour commencer le jeu.";
            }

            IsStarted = true;
            GenerateRandomText();
            return null; // Aucun message d'erreur, le jeu peut commencer
        }

        // Calculer le score d'un joueur en fonction de l'entrée utilisateur
        public int CalculateScore(string player, string userInput)
        {
            if (!Players.Contains(player))
            {
                throw new InvalidOperationException("Le joueur n'existe pas dans ce lobby.");
            }

            if (!IsStarted)
            {
                throw new InvalidOperationException("Le jeu n'a pas encore commencé.");
            }

            // Normalisation des chaînes pour comparaison insensible aux accents
            string normalizedTextToType = TextToType.Normalize(NormalizationForm.FormD);
            string normalizedUserInput = userInput.Normalize(NormalizationForm.FormD);

            // Comparaison sensible à la casse (suppression de IgnoreCase)
            if (string.Compare(normalizedTextToType, normalizedUserInput, CultureInfo.InvariantCulture, CompareOptions.IgnoreNonSpace) == 0)
            {
                Progress[player] = 100; // 100% pour marquer la fin
                return 1000; // Score pour avoir terminé
            }
            else if (normalizedTextToType.StartsWith(normalizedUserInput, StringComparison.Ordinal)) // Ordinal est sensible à la casse
            {
                return userInput.Length * 10; // Score basé sur la longueur correcte
            }

            return 0; // Aucun score si l'entrée ne correspond pas
        }

        // Générer un texte aléatoire à taper
        private void GenerateRandomText()
        {
            var random = new Random();
            var selectedWords = new List<string>();
            int numberOfWords = random.Next(20, 31); // Entre 20 et 30 mots

            for (int i = 0; i < numberOfWords; i++)
            {
                int index = random.Next(FrenchWords.Count);
                selectedWords.Add(FrenchWords[index]);
            }

            TextToType = string.Join(" ", selectedWords);
        }

        // Obtenir les joueurs triés par progression décroissante
        public List<KeyValuePair<string, int>> GetLeaderboard()
        {
            return new List<KeyValuePair<string, int>>(Progress.OrderByDescending(p => p.Value));
        }
    }
}
