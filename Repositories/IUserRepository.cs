using Backend.Models;
namespace Backend.Repositories
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task<User?> GetUserByIdAsync(Guid userId);
    }
}
