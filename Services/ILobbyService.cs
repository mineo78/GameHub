using Backend.Enums;
using Backend.Models;

namespace Backend.Services
{
    public interface ILobbyService
    {
        Task<Lobby> CreateLobbyAsync(Guid hostId, string lobbyName, GameType type);

        Task<Lobby?> JoinLobbyAsync(Guid lobbyId, Guid userId);

        Task<Lobby?> GetLobbyByIdAsync(Guid lobbyId);

        Task<IEnumerable<Lobby>> GetAvailableLobbiesAsync();

        Task<Lobby?> StartGameAsync(Guid lobbyId);
    }
}
