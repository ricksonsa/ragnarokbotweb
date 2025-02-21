using Microsoft.EntityFrameworkCore;
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
            _appDbContext.Packs.Attach(entity.Pack);
            return base.AddAsync(entity);
        }

        public Task<Order?> FindOneWithPackCreated()
        {
            return _appDbContext.Orders
                .Include(order => order.User)
                .Include(order => order.Pack)
                .ThenInclude(pack => pack!.PackItems)
                .ThenInclude(packItems => packItems.Item)
                .OrderBy(order => order.CreateDate)
                .FirstOrDefaultAsync(order => order.Status == EOrderStatus.Created);
        }
    }
}
