
using Backend.Enums;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services
{

    public class LobbyService(ILobbyRepository lobbyRepository) : ILobbyService
    {
        private readonly ILobbyRepository _lobbyRepository = lobbyRepository;

        public async Task<Lobby> CreateLobbyAsync(Guid hostId, string lobbyName, GameType type)
        {
            // 1. Créer le nouveau Lobby
            var lobby = new Lobby
            {
                Id = Guid.NewGuid(),
                HostId = hostId,
                Name = lobbyName,
                GameType = type,
                // C'est un lobby public par défaut, IsPrivate=false implicite
            };
            lobby.PlayerIds.Add(hostId); // L'hôte rejoint automatiquement

            // 2. Sauvegarder dans la DB
            await _lobbyRepository.AddLobbyAsync(lobby);

            // 3. (Futur) Notifier SignalR ici que la liste des lobbies a changé

            return lobby;
        }

        public Task<IEnumerable<Lobby>> GetAvailableLobbiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Lobby?> GetLobbyByIdAsync(Guid lobbyId)
        {
            throw new NotImplementedException();
        }

        public async Task<Lobby?> JoinLobbyAsync(Guid lobbyId, Guid userId)
        {
            var lobby = await _lobbyRepository.GetLobbyByIdAsync(lobbyId);

            if (lobby == null || lobby.Status != LobbyStatus.WaitingForPlayers) return null;

            if (!lobby.PlayerIds.Contains(userId))
            {
                lobby.PlayerIds.Add(userId);
                await _lobbyRepository.UpdateLobbyAsync(lobby);
                // 4. (Futur) Notifier SignalR ici que le lobby a un nouveau joueur
            }
            return lobby;
        }

        public Task<Lobby?> StartGameAsync(Guid lobbyId)
        {
            throw new NotImplementedException();
        }

    }
}
