using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GamingPlatform.Models;

namespace GamingPlatform.Services
{
    /// <summary>
    /// Service pour gérer l'historique des actions de jeu
    /// Permet de tracer toutes les actions pour les contestations
    /// </summary>
    public class GameHistoryService
    {
        private readonly ConcurrentDictionary<string, GameHistory> _histories = new();
        private readonly string _historyFilePath;

        public GameHistoryService()
        {
            _historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_histories.json");
            LoadHistories();
        }

        /// <summary>
        /// Démarre l'enregistrement d'une nouvelle partie
        /// </summary>
        public void StartGame(string lobbyId, string lobbyName, string gameType, List<string> players)
        {
            var history = new GameHistory
            {
                LobbyId = lobbyId,
                LobbyName = lobbyName,
                GameType = gameType,
                CreatedAt = DateTime.UtcNow,
                Players = new List<string>(players)
            };

            _histories[lobbyId] = history;

            // Enregistrer l'action de démarrage
            LogAction(lobbyId, gameType, "SYSTEM", "GAME_START", 
                $"Partie démarrée avec les joueurs: {string.Join(", ", players)}");
            
            SaveHistories();
        }

        /// <summary>
        /// Enregistre une action dans l'historique
        /// </summary>
        public void LogAction(string lobbyId, string gameType, string playerName, string actionType, string details, object? additionalData = null)
        {
            if (!_histories.TryGetValue(lobbyId, out var history))
            {
                // Créer un historique si inexistant
                history = new GameHistory
                {
                    LobbyId = lobbyId,
                    GameType = gameType,
                    CreatedAt = DateTime.UtcNow
                };
                _histories[lobbyId] = history;
            }

            var action = new GameAction
            {
                LobbyId = lobbyId,
                GameType = gameType,
                PlayerName = playerName,
                ActionType = actionType,
                ActionDetails = details,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            history.Actions.Add(action);
            SaveHistories();
        }

        /// <summary>
        /// Termine une partie et enregistre le résultat
        /// </summary>
        public void EndGame(string lobbyId, string? winner, bool isTie)
        {
            if (_histories.TryGetValue(lobbyId, out var history))
            {
                history.EndedAt = DateTime.UtcNow;
                history.Winner = winner;
                history.IsTie = isTie;

                var resultMessage = isTie ? "Match nul" : $"Victoire de {winner}";
                LogAction(lobbyId, history.GameType, "SYSTEM", "GAME_END", resultMessage);
                
                SaveHistories();
            }
        }

        /// <summary>
        /// Récupère l'historique d'une partie
        /// </summary>
        public GameHistory? GetHistory(string lobbyId)
        {
            _histories.TryGetValue(lobbyId, out var history);
            return history;
        }

        /// <summary>
        /// Récupère tous les historiques (pour l'admin)
        /// </summary>
        public IEnumerable<GameHistory> GetAllHistories()
        {
            return _histories.Values.OrderByDescending(h => h.CreatedAt);
        }

        /// <summary>
        /// Recherche des historiques par critères
        /// </summary>
        public IEnumerable<GameHistory> SearchHistories(string? gameType = null, string? playerName = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _histories.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(gameType))
                query = query.Where(h => h.GameType == gameType);

            if (!string.IsNullOrEmpty(playerName))
                query = query.Where(h => h.Players.Any(p => p.Contains(playerName, StringComparison.OrdinalIgnoreCase)));

            if (fromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(h => h.CreatedAt <= toDate.Value);

            return query.OrderByDescending(h => h.CreatedAt);
        }

        /// <summary>
        /// Exporte l'historique d'une partie au format JSON
        /// </summary>
        public string ExportHistoryJson(string lobbyId)
        {
            if (_histories.TryGetValue(lobbyId, out var history))
            {
                return JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            }
            return "{}";
        }

        private void SaveHistories()
        {
            try
            {
                var json = JsonSerializer.Serialize(_histories.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde des historiques: {ex.Message}");
            }
        }

        private void LoadHistories()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var histories = JsonSerializer.Deserialize<List<GameHistory>>(json);
                    if (histories != null)
                    {
                        foreach (var history in histories)
                        {
                            _histories[history.LobbyId] = history;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des historiques: {ex.Message}");
            }
        }
    }
}
