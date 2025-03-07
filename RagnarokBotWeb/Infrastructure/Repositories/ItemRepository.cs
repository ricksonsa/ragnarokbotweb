using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        private readonly AppDbContext _appDbContext;
        public ItemRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public Task<Page<Item>> GetPageByFilter(Paginator paginator, string? filter)
        {
            var query = _appDbContext.Items;

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(item => item.Name.ToLower().Contains(filter) || item.Code.ToLower().Contains(filter)));
            }

            return base.GetPageAsync(paginator, query);
        }
    }
}
