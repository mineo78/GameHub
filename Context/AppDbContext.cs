using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace Backend.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<Lobby> Lobbies { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lobby>(entity =>
            {

                entity.Property(l => l.PlayerIds)
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                          v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)
                              ?? new List<Guid>()
                      );

                entity.Property(l => l.GameType)
                      .HasConversion<string>();

                entity.Property(l => l.Status)
                      .HasConversion<string>();
            });
        }
    }
}
