using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface ICustomTaskRepository : IRepository<CustomTask>
    {
        Task<List<CustomTask>> GetServersEnabledCustomTasks();
        Task<Page<CustomTask>> GetPageByServerAndFilter(Paginator paginator, long id, string? filter);
    }
}
