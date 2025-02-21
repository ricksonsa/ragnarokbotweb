using RagnarokBotWeb.Domain.Entities;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> FindBySteamId64Async(string steamId);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task UpdateUserNameAsync(string steamId64, string name);
        Task UpdateFromPlayers(List<Player>? players);
    }
}
