using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBotService
    {
        Task CheckBotState();
        Task<Bot> RegisterBot(string steamId64);
        Task<Bot?> UnregisterBot(string identifier);
        Task UpdateInteraction(string steamId64);
        Task UpdatePlayersOnline(string input, string identifier);
    }
}
