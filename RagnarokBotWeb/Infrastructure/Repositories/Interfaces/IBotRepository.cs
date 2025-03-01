using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IBotRepository : IRepository<Bot>
    {
        Task<Bot?> FindByScumServerId(long id);
    }
}
