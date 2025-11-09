namespace Backend.Models
{
    public class User
    {
        public Guid Id
        {
            get; set;
        }

        public required string Pseudo { get; set; }
    }
}