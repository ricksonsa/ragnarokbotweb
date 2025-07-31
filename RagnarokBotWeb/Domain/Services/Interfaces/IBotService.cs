using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBotService
    {
        void ConnectBot(Guid guid);
        void DisconnectBot(Guid guid);
        Task UpdatePlayersOnline(PlayersListRequest input);
        bool IsBotOnline();
        bool IsBotOnline(long serverId);
        BotCommand? GetCommand(Guid guid);
        void PutCommand(BotCommand command);
        Task ConfirmDelivery(long orderId);
        List<BotUser> FindActiveBotsByServerId(long serverId);
        void ResetBotState(long value);
        BotUser? FindBotByGuid(Guid guid);
        void RegisterBot(Guid guid);
    }
}
