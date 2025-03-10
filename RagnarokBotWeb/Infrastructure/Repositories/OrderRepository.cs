using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        private readonly AppDbContext _appDbContext;
        public OrderRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public override Task AddAsync(Order entity)
        {
            if (entity.Pack is not null) _appDbContext.Packs.Attach(entity.Pack);
            return base.AddAsync(entity);
        }

        public override Task CreateOrUpdateAsync(Order entity)
        {
            if (entity.ScumServer is not null) _appDbContext.ScumServers.Attach(entity.ScumServer);
            if (entity.Pack is not null) _appDbContext.Packs.Attach(entity.Pack);

            return base.CreateOrUpdateAsync(entity);
        }

        public Task<Order?> FindOneWithPackCreated()
        {
            return _appDbContext.Orders
                .Include(order => order.Player)
                .Include(order => order.Pack)
                .ThenInclude(pack => pack!.PackItems)
                .ThenInclude(packItems => packItems.Item)
                .OrderBy(order => order.CreateDate)
                .FirstOrDefaultAsync(order => order.Status == EOrderStatus.Created);
        }

        public Task<Page<Order>> GetPageByFilter(Paginator paginator, string? filter)
        {
            var query = _appDbContext.Orders
               .Include(pack => pack.Pack)
               .Include(pack => pack.ScumServer)
               .Include(pack => pack.Player);

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(
                    order =>
                    order.Pack != null && (order.Pack.Name.ToLower().Contains(filter) || order.Pack.Description.ToLower().Contains(filter))
                    || order.Id.ToString() == filter));
            }

            return base.GetPageAsync(paginator, query);
        }
    }
}
