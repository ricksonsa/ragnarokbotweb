using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Business
{
    public class OrderPurchaseProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICacheService _cacheService;

        public OrderPurchaseProcessor(IOrderRepository orderRepository, ICacheService cacheService)
        {
            _orderRepository = orderRepository;
            _cacheService = cacheService;
        }

        private bool WasPurchasedWithinPurchaseCooldownSeconds(Order order)
        {
            if (order.OrderType == Shared.Enums.EOrderType.Warzone)
            {
                return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.Warzone!.PurchaseCooldownSeconds;
            }
            else if (order.OrderType == Shared.Enums.EOrderType.Pack)
            {
                return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.Pack!.PurchaseCooldownSeconds;
            }
            return false;
        }

        private async Task ValidatePack(Order order)
        {
            if (!order.Pack!.Enabled || order.Pack.Deleted.HasValue)
            {
                throw new DomainException("This pack is not available at the moment.");
            }

            if (order.Pack.IsVipOnly && !order.Player!.IsVip())
            {
                throw new DomainException("This pack is only available for Vip Players.");
            }

            if (order.Pack.IsWelcomePack)
            {
                throw new DomainException("This pack is only available at registration.");
            }

            var previousSamePackOrders = await _orderRepository.FindWithPack(order.Pack!.Id);
            if (order.Pack.PurchaseCooldownSeconds.HasValue)
            {
                var previousOrder = previousSamePackOrders.FirstOrDefault(WasPurchasedWithinPurchaseCooldownSeconds);
                if (previousOrder != null)
                {
                    throw new DomainException($"This pack is under cooldown time. {previousOrder.ResolvePackCooldownText()}");
                }
            }

            if (previousSamePackOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Pack.StockPerPlayer)
            {
                throw new DomainException("This pack is out of stock, try again later.");
            }

            if (order.Pack.IsBlockPurchaseRaidTime)
            {
                var raidTimes = _cacheService.GetRaidTimes(order.ScumServer.Id);
                if (raidTimes is not null && raidTimes.IsInRaidTime(order.ScumServer))
                {
                    throw new DomainException("This Pack cannot be purchased during Raid Period.");
                }
            }

            if (order.Player!.Coin < order.ResolvedPrice) throw new DomainException("You don't have enough bot coins.");
        }

        private async Task ValidateWarzone(Order order)
        {
            if (!order.Warzone!.Enabled || order.Warzone.Deleted.HasValue)
            {
                throw new DomainException("This Warzone is not available at the moment.");
            }

            if (order.Warzone.IsVipOnly && !order.Player!.IsVip())
            {
                throw new DomainException("This Warzone Teleport is only available for Vip Players.");
            }

            var previousSameWarzoneOrders = await _orderRepository.FindWithWarzone(order.Warzone!.Id);
            if (order.Warzone.PurchaseCooldownSeconds.HasValue)
            {
                var previousOrder = previousSameWarzoneOrders.FirstOrDefault(WasPurchasedWithinPurchaseCooldownSeconds);
                if (previousOrder != null)
                {
                    throw new DomainException($"This Warzone Teleport is under cooldown time. {previousOrder.ResolveWarzoneCooldownText()}");
                }
            }

            if (previousSameWarzoneOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Warzone.StockPerPlayer)
            {
                throw new DomainException("This Warzone Teleport is out of stock, try again later.");
            }

            if (order.Warzone.IsBlockPurchaseRaidTime)
            {
                var raidTimes = _cacheService.GetRaidTimes(order.ScumServer.Id);
                if (raidTimes is not null && raidTimes.IsInRaidTime(order.ScumServer))
                {
                    throw new DomainException("This Warzone Teleport cannot be purchased during Raid Period.");
                }
            }

            var price = order.Player!.IsVip() ? Convert.ToInt64(order.Warzone!.VipPrice) : Convert.ToInt64(order.Warzone!.Price);
            if (order.Player.Coin < price) throw new DomainException("You don't have enough bot coins.");
        }

        public async Task ValidateAsync(Order order)
        {
            if (order.Player is null)
            {
                throw new DomainException("Player not yet registered, please register using the Welcome Pack.");
            }

            if (order.OrderType == Shared.Enums.EOrderType.Pack)
            {
                await ValidatePack(order);
            }
            else if (order.OrderType == Shared.Enums.EOrderType.Warzone)
            {
                await ValidateWarzone(order);
            }
        }

        public bool WasPurchasedWithinLast24Hours(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalHours <= 24;
        }
    }
}
