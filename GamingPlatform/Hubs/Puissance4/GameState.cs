using GamingPlatform.Models.Puissance4;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GamingPlatform.Hubs.Puissance4
{
    public class GameState
    {
        private readonly ConcurrentDictionary<string, Player> players = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Game> games = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<Player> waitingPlayers = new();
        private readonly IHubContext<Puissance4Hub> _hubContext;

        public GameState(IHubContext<Puissance4Hub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Player CreatePlayer(string username, string connectionId)
        {
            var player = new Player(username, connectionId);
            players[connectionId] = player;
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

            opponent = player.Id == foundGame.Player1.Id ? foundGame.Player2 : foundGame.Player1;
            return foundGame;
        }

        public Game GetGameById(string gameId)
        {
            games.TryGetValue(gameId, out var foundGame);
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

        public void RemoveGameIfExists(string gameId)
        {
            if (games.TryRemove(gameId, out var foundGame))
            {
                players.TryRemove(foundGame.Player1.Id, out _);
                players.TryRemove(foundGame.Player2.Id, out _);
            }
        }

        public void AddToWaitingPool(Player player) => waitingPlayers.Enqueue(player);

        public bool IsUsernameTaken(string username)
        {
            return players.Values.Any(p => p.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<Game> CreateGame(Player firstPlayer, Player secondPlayer)
        {
            var game = new Game(firstPlayer, secondPlayer);
            games[game.Id] = game;

            await _hubContext.Groups.AddToGroupAsync(firstPlayer.Id, game.Id);
            await _hubContext.Groups.AddToGroupAsync(secondPlayer.Id, game.Id);

            return game;
        }
    }
}
