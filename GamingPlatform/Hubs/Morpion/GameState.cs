using GamingPlatform.Models.Morpion;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GamingPlatform.Hubs.Morpion
{
    /// <summary>
    /// This class persists a collection of players and matches, using DI for better modularity.
    /// </summary>
    public class GameState
    {
        private readonly ConcurrentDictionary<string, Player> players =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Game> games =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentQueue<Player> waitingPlayers = new();

        private readonly IHubContext<MorpionHub> _hubContext;

       
        public GameState(IHubContext<MorpionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Player CreatePlayer(string username, string connectionId)
        {
            var player = new Player(username, connectionId);
            players[connectionId] = player;
            Console.WriteLine($"Player created: {username}, ConnectionId: {connectionId}");
            return player;
        }

        public Player GetPlayer(string playerId)
        {
            players.TryGetValue(playerId, out var foundPlayer);
            return foundPlayer;
        }

        public Game GetGame(Player player, out Player opponent)
        {
            opponent = null;

            var foundGame = games.Values.FirstOrDefault(g => g.Id == player.GameId);
            if (foundGame == null) return null;

            opponent = player.Id == foundGame.Player1.Id
                ? foundGame.Player2
                : foundGame.Player1;

            return foundGame;
        }

        public Player GetWaitingOpponent()
        {
            waitingPlayers.TryDequeue(out var foundPlayer);
            return foundPlayer;
        }

        public void RemoveGame(string gameId)
        {
            if (!games.TryRemove(gameId, out var foundGame))
            {
                throw new InvalidOperationException("Game not found.");
            }

            players.TryRemove(foundGame.Player1.Id, out _);
            players.TryRemove(foundGame.Player2.Id, out _);
        }

        /// <summary>
        /// Supprime un jeu s'il existe (sans erreur si non trouvé)
        /// </summary>
        public void RemoveGameIfExists(string gameId)
        {
            if (games.TryRemove(gameId, out var foundGame))
            {
                players.TryRemove(foundGame.Player1.Id, out _);
                players.TryRemove(foundGame.Player2.Id, out _);
            }
            // Nettoyer aussi la file d'attente du lobby
            _lobbyWaiting.TryRemove(gameId, out _);
        }

        public void AddToWaitingPool(Player player)
        {
            waitingPlayers.Enqueue(player);
        }

        public bool IsUsernameTaken(string username)
        {
            return players.Values.Any(player =>
                player.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase));
        }

        private readonly ConcurrentDictionary<string, Player> _lobbyWaiting = new();

        public Player RegisterLobbyPlayer(string lobbyId, Player player)
        {
            if (_lobbyWaiting.TryRemove(lobbyId, out var opponent))
            {
                return opponent;
            }
            _lobbyWaiting.TryAdd(lobbyId, player);
            return null;
        }

        public async Task<Game> CreateGame(Player firstPlayer, Player secondPlayer, string gameId = null)
        {
            var game = new Game(firstPlayer, secondPlayer, gameId);
            
            games[game.Id] = game;

            await _hubContext.Groups.AddToGroupAsync(firstPlayer.Id, game.Id);
            await _hubContext.Groups.AddToGroupAsync(secondPlayer.Id, game.Id);

            return game;
        }

        public Game GetGame(string gameId)
        {
            games.TryGetValue(gameId, out var game);
            return game;
        }

    
    
    }
}
