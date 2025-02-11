using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public Task<User> FindBySteamId64Async(string steamId64)
        {
            return _userRepository.FindOneAsync(user => user.SteamId64 == steamId64);
        }

        public async Task AddUserAsync(User user)
        {
            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _userRepository.Update(user);
            await _userRepository.SaveAsync();
        }
    }
}
