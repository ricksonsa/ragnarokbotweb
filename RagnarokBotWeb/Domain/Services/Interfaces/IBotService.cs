using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBotService
    {
        Task ConnectBot(Guid guid);
        void DisconnectBot(Guid guid);
        bool IsBotOnline();
        bool IsBotOnline(long serverId);
        Task<BotCommand?> GetCommand(Guid guid);
        void PutCommand(BotCommand command);
        Task ConfirmDelivery(long orderId);
        List<BotUser> FindActiveBotsByServerId(long serverId);
        void ResetBotState(long value);
        Task<BotUser?> FindBotByGuid(Guid guid);
        Task RegisterBot(Guid guid);

        Task UpdatePlayersOnline(UpdateFromStringRequest input);
        Task UpdateFlags(UpdateFromStringRequest input);
        Task UpdateSquads(UpdateFromStringRequest input);
        List<BotUser> GetBots();
    }
}
