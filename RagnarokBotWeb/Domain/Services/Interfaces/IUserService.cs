using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> FindBySteamId64Async(string steamId);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
    }
}
