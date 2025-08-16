using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class CustomTaskRepository(AppDbContext context) : Repository<CustomTask>(context), ICustomTaskRepository
    {

        public override async Task<CustomTask?> FindByIdAsync(long id)
        {
            return await DbSet()
                .Include(task => task.ScumServer)
                .FirstOrDefaultAsync(task => task.Id == id);
        }

        public Task<Page<CustomTask>> GetPageByServerAndFilter(Paginator paginator, long id, string? filter)
        {
            var query = DbSet()
                .Include(task => task.ScumServer)
                .OrderByDescending(task => task.Id)
                .Where(task => task.ScumServer.Id == id);

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(task => task.Name.ToLower().Contains(filter)
                || task.Description != null && task.Description.ToLower().Contains(filter)));
            }

            return base.GetPageAsync(paginator, query);
        }

        public Task<List<CustomTask>> GetServersEnabledCustomTasks()
        {
            return DbSet()
                .Include(task => task.ScumServer)
                .Where(task => task.Enabled)
                .ToListAsync();
        }
    }
}
