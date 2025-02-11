using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPlayerService
    {
        bool IsPlayerConnected(string steamId64);
        Task PlayerConnected(string steamId64, string scumId, string name);
        User PlayerDisconnected(string steamId64);
    }
}
