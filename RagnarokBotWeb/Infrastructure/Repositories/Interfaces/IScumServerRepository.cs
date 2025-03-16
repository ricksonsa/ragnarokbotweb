using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IScumServerRepository : IRepository<ScumServer>
    {
        Task<ScumServer?> FindActiveById(long id);
        Task<List<ScumServer>> FindManyByTenantIdAsync(long id);
        Task<ScumServer?> FindOneByTenantIdAsync(long id);
        Task<List<ScumServer>> FindActive();
        Task<List<ScumServer>> GetActiveServersWithFtp();
        Task<ScumServer?> FindByIdAsNoTrackingAsync(long id);
    }
}
