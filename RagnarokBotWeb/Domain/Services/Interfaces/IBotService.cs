using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBotService
    {
        Task CheckBotState();
        Task<Bot> RegisterBot();
        Task<Bot?> UnregisterBot();
        Task UpdateInteraction();
        Task<BotCommand?> GetCommand();
        Task PutCommand(BotCommand command);
        Task UpdatePlayersOnline(string input);
    }
}
