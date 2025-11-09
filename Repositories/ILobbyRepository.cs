using Backend.Models;

namespace Backend.Repositories
{

    public interface ILobbyRepository
    {
        Task AddLobbyAsync(Lobby lobby);
        Task<Lobby?> GetLobbyByIdAsync(Guid lobbyId);
        Task<IEnumerable<Lobby>> GetAllWaitingLobbiesAsync();
        Task UpdateLobbyAsync(Lobby lobby);
    }
}
