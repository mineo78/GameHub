using Backend.Models;
using Backend.Repositories;

namespace Backend.Services
{
    public class UserService(IUserRepository userRepository) : IUserService
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<User> CreateUserAsync(string pseudo)
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Pseudo = pseudo
            };

            await _userRepository.AddUserAsync(newUser);
            return newUser;
        }
    }
}
