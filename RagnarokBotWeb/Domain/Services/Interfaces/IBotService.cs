using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBotService
    {
        Task CheckBotState(long serverId);
        Task CheckBotState(ulong guildId);
        Task<List<Bot>> FindActiveBotsByServerId(long serverId);
        Task<BotDto> RegisterBot();
        Task ConfirmDelivery(long orderId);
        Task<BotDto?> UnregisterBot();
        Task UpdateInteraction();
        Task<BotCommand?> GetCommand();
        Task PutCommand(BotCommand command);
        Task UpdatePlayersOnline(PlayersListRequest input);
        Task<bool> IsBotOnline(ulong guildId);
    }
}
