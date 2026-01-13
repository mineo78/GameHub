using System;

namespace GamingPlatform.Models.Puissance4
{
    public class Player
    {
        public Player(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; private set; }
        public string Id { get; private set; }
        public string GameId { get; set; }
        public string Color { get; set; }

        public override string ToString() => $"(Id={Id}, Name={Name}, GameId={GameId}, Color={Color})";

        public override bool Equals(object obj)
        {
            if (obj is not Player other) return false;
            return Id.Equals(other.Id) && Name.Equals(other.Name);
        }

        public override int GetHashCode() => HashCode.Combine(Id, Name);
    }
}
