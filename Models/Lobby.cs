using Backend.Enums;

namespace Backend.Models
{
    public class Lobby
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public Guid HostId { get; set; }

        public GameType GameType { get; set; }

        public List<Guid> PlayerIds { get; set; } = [];

        // Statut simple : En attente de joueurs / En cours.
        public LobbyStatus Status { get; set; } = LobbyStatus.WaitingForPlayers;

        public Guid? GameId { get; set; }
    }
}
