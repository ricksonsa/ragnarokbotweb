using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IBotRepository : IRepository<Bot>
    {
        Task<Bot?> FindByScumServerId(long id);
        Task<List<Bot>> FindByServerIdOnlineAndLastInteraction(long id);
        Task<List<Bot>> FindActiveBotsByServerId(long id);
    }
}
