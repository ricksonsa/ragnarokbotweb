using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IScumServerRepository : IRepository<ScumServer>
    {
        Task<List<ScumServer>> FindByTenantIdAsync(long id);
        Task<List<ScumServer>> GetActiveServersWithFtp();
    }
}
