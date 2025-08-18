using RagnarokBotWeb.Application.Models;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBotService
    {
        bool IsBotOnline();
        bool IsBotOnline(long serverId);
        Task ConfirmDelivery(long orderId);
        List<BotUser> FindActiveBotsByServerId(long serverId);
        Task ResetBotState(long value);
        Task<BotUser?> FindBotByGuid(Guid guid);
        Task SendCommand(long serverId, BotCommand command);

        Task UpdatePlayersOnline(UpdateFromStringRequest input);
        Task UpdateFlags(UpdateFromStringRequest input);
        Task UpdateSquads(UpdateFromStringRequest input);
        List<BotUser> GetBots();
        List<BotUser> GetConnectedBots();
    }
}
