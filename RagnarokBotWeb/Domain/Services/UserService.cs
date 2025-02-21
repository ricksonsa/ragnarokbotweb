using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;

        public UserService(IUserRepository userRepository, ICacheService cacheService)
        {
            _userRepository = userRepository;
            _cacheService = cacheService;
        }

        public async Task UpdateFromPlayers(List<Player>? players)
        {
            if (players is null) players = _cacheService.GetConnectedPlayers().Values.ToList();
            foreach (var player in players)
            {
                var user = await FindBySteamId64Async(player.SteamID);
                user ??= new();
                user.X = player.X;
                user.Y = player.Y;
                user.Z = player.Z;
                user.Name = player.Name;
                user.SteamId64 = player.SteamID;
                user.SteamName = player.SteamName;
                user.Gold = player.GoldBalance;
                user.Money = player.AccountBalance;
                user.Fame = player.Fame;
                _userRepository.Update(user);
            }
            await _userRepository.SaveAsync();
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

        public async Task UpdateUserNameAsync(string steamId64, string name)
        {
            var user = await FindBySteamId64Async(steamId64);
            user.Name = name;
            _userRepository.Update(user);
            await _userRepository.SaveAsync();
        }
    }
}
