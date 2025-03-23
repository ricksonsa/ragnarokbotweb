using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IBunkerService
    {
        Task UpdateBunkerState(ScumServer server, string sector, bool locked, TimeSpan activation);
    }
}
