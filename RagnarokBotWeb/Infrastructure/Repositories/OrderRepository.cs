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
            if (entity.Player is not null) _appDbContext.Players.Attach(entity.Player);

            return base.CreateOrUpdateAsync(entity);
        }

        public Task<Order?> FindOneByServer(long serverId)
        {
            return _appDbContext.Orders
                .Include(order => order.ScumServer)
                .Include(order => order.ScumServer.Uav)
                .Include(order => order.Player)
                .Include(order => order.Pack)
                .ThenInclude(pack => pack.PackItems)
                .ThenInclude(packItems => packItems.Item)
                .Include(order => order.Warzone)
                    .ThenInclude(warzone => warzone.Teleports)
                    .ThenInclude(teleport => teleport.Teleport)
                .Where(order => order.ScumServer != null && order.ScumServer.Id == serverId)
                .OrderBy(order => order.CreateDate)
                .FirstOrDefaultAsync(order => order.Status == EOrderStatus.Created);
        }

        public Task<List<Order>> FindManyByServer(long serverId)
        {
            return _appDbContext.Orders
                .Include(order => order.ScumServer)
                .Include(order => order.ScumServer.Uav)
                .Include(order => order.Player)
                .Include(order => order.Pack)
                .ThenInclude(pack => pack.PackItems)
                .ThenInclude(packItems => packItems.Item)
                .Include(order => order.Warzone)
                    .ThenInclude(warzone => warzone.Teleports)
                    .ThenInclude(teleport => teleport.Teleport)
                .Where(order => order.ScumServer != null && order.ScumServer.Id == serverId && order.Status == EOrderStatus.Created)
                .OrderBy(order => order.CreateDate)
                .ToListAsync();
        }

        public Task<List<Order>> FindWithPack(long packId)
        {
            var now = DateTime.UtcNow;
            return _appDbContext.Orders
                .Include(order => order.ScumServer)
                .Include(order => order.Player)
                .Include(order => order.Pack)
                .ThenInclude(pack => pack!.PackItems)
                .ThenInclude(packItems => packItems.Item)
                .Where(order => order.Pack != null && order.Pack.Id == packId && order.CreateDate >= now.AddHours(-24) && order.CreateDate <= now)
                .OrderByDescending(order => order.CreateDate)
                .ToListAsync();
        }

        public Task<List<Order>> FindWithWarzone(long warzoneId)
        {
            var now = DateTime.UtcNow;
            return _appDbContext.Orders
                .Include(order => order.ScumServer)
                .Include(order => order.Player)
                .Include(order => order.Warzone)
                    .ThenInclude(warzone => warzone!.Teleports)
                    .ThenInclude(teleport => teleport.Teleport)
                .Where(order => order.Warzone != null && order.Warzone.Id == warzoneId && order.CreateDate >= now.AddHours(-24) && order.CreateDate <= now)
                .OrderByDescending(order => order.CreateDate)
                .ToListAsync();
        }

        public Task<Page<Order>> GetPageByFilter(long serverId, Paginator paginator, string? filter)
        {
            var query = _appDbContext.Orders
               .Include(order => order.Pack)
               .Include(order => order.Warzone)
               .Include(order => order.ScumServer)
               .Include(order => order.ScumServer.Uav)
               .Include(order => order.Player)
               .Where(order => order.ScumServer.Id == serverId)
               .OrderByDescending(order => order.Id);

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(
                    order =>
                    order.Player != null && (order.Player.Name != null && order.Player.Name.ToLower().Contains(filter) || (order.Player.SteamId64 != null && order.Player.SteamId64 == filter)
                    || order.Pack != null && (order.Pack.Name.ToLower().Contains(filter) || (order.Pack.Description != null && order.Pack.Description.ToLower().Contains(filter)))
                    || order.Warzone != null && (order.Warzone.Name.ToLower().Contains(filter) || (order.Warzone.Description != null && order.Warzone.Description.ToLower().Contains(filter)))
                    || order.Id.ToString() == filter)));
            }

            return base.GetPageAsync(paginator, query);
        }
    }
}
