using Backend.Context;
using Backend.Enums;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    public class LobbyRepository(AppDbContext context) : ILobbyRepository
    {
        private readonly AppDbContext _context = context;

        public async Task AddLobbyAsync(Lobby lobby)
        {
            _context.Lobbies.Add(lobby);
            await _context.SaveChangesAsync();
        }

        public async Task<Lobby?> GetLobbyByIdAsync(Guid lobbyId)
        {
            return await _context.Lobbies.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lobbyId);
        }

        public async Task<IEnumerable<Lobby>> GetAllWaitingLobbiesAsync()
        {
            return await _context.Lobbies
                .AsNoTracking()
                .Where(l => l.Status == LobbyStatus.WaitingForPlayers) // Assurez-vous d'avoir l'enum
                .ToListAsync();
        }

        public async Task UpdateLobbyAsync(Lobby lobby)
        {
            _context.Lobbies.Update(lobby);
            await _context.SaveChangesAsync();
        }
    }
}
