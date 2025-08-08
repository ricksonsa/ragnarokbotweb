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
        private Player _player;

        public OrderPurchaseProcessor(IOrderRepository orderRepository, ICacheService cacheService)
        {
            _orderRepository = orderRepository;
            _cacheService = cacheService;
        }

        private bool WasPurchasedWithinPurchaseCooldownSeconds(Order order)
        {
            if (order.OrderType == Shared.Enums.EOrderType.Warzone)
                return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.Warzone!.PurchaseCooldownSeconds && order.Player!.Id == _player.Id;
            else if (order.OrderType == Shared.Enums.EOrderType.Pack)
                return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.Pack!.PurchaseCooldownSeconds && order.Player!.Id == _player.Id;
            else if (order.OrderType == Shared.Enums.EOrderType.UAV)
                return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.ScumServer.Uav!.PurchaseCooldownSeconds;

            return false;
        }

        private async Task ValidatePack(Order order)
        {
            if (!order.Pack!.Enabled || order.Pack.Deleted.HasValue)
                throw new DomainException("This pack is not available at the moment.");

            if (order.Pack.IsVipOnly && !order.Player!.IsVip())
                throw new DomainException("This pack is only available for Vip Players.");

            if (order.Pack.IsWelcomePack)
                throw new DomainException("This pack is only available at registration.");

            var previousSamePackOrders = await _orderRepository.FindWithPack(order.Pack!.Id);
            if (order.Pack.PurchaseCooldownSeconds.HasValue)
            {
                var previousOrder = previousSamePackOrders.FirstOrDefault(WasPurchasedWithinPurchaseCooldownSeconds);
                if (previousOrder != null)
                    throw new DomainException($"This pack is under cooldown time. {previousOrder.ResolvePackCooldownText()}");
            }

            if (order.Player!.IsVip() && previousSamePackOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Pack.StockPerVipPlayer)
                throw new DomainException("This pack is out of stock, try again later.");

            if (!order.Player!.IsVip() && previousSamePackOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Pack.StockPerPlayer)
                throw new DomainException("This pack is out of stock, try again later.");

            if (order.Pack.IsBlockPurchaseRaidTime)
            {
                var raidTimes = _cacheService.GetRaidTimes(order.ScumServer.Id);
                if (raidTimes is not null && raidTimes.IsInRaidTime(order.ScumServer))
                    throw new DomainException("This Pack cannot be purchased during Raid Period.");
            }
        }

        private async Task ValidateWarzone(Order order)
        {
            if (order.Warzone.IsVipOnly && !order.Player!.IsVip())
                throw new DomainException("This Warzone Teleport is only available for Vip Players.");

            var previousSameWarzoneOrders = await _orderRepository.FindWithWarzone(order.Warzone!.Id);
            if (order.Warzone.PurchaseCooldownSeconds.HasValue)
            {
                var previousOrder = previousSameWarzoneOrders.FirstOrDefault(WasPurchasedWithinPurchaseCooldownSeconds);
                if (previousOrder != null)
                    throw new DomainException($"This Warzone Teleport is under cooldown time. {previousOrder.ResolveWarzoneCooldownText()}");
            }

            if (order.Player!.IsVip() && previousSameWarzoneOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Warzone.StockPerVipPlayer)
                throw new DomainException("This Warzone Teleport is out of stock, try again later.");

            if (!order.Player!.IsVip() && previousSameWarzoneOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Warzone.StockPerPlayer)
                throw new DomainException("This Warzone Teleport is out of stock, try again later.");

            if (order.Warzone.IsBlockPurchaseRaidTime)
            {
                var raidTimes = _cacheService.GetRaidTimes(order.ScumServer.Id);
                if (raidTimes is not null && raidTimes.IsInRaidTime(order.ScumServer))
                    throw new DomainException("This Warzone Teleport cannot be purchased during Raid Period.");
            }
        }

        private async Task ValidateUav(Order order)
        {
            if (!order.ScumServer.Uav.Enabled)
                throw new DomainException("UAV is not available at the moment.");

            if (order.ScumServer.Uav.IsVipOnly && !order.Player!.IsVip())
                throw new DomainException("UAV Teleport is only available for Vip Players.");

            var now = DateTime.Now;
            var previousSameWarzoneOrders = await _orderRepository.FindAsync(order => order.OrderType == Shared.Enums.EOrderType.UAV
            && order.CreateDate >= now.AddHours(-24) && order.CreateDate <= now);

            if (order.ScumServer.Uav.PurchaseCooldownSeconds.HasValue)
            {
                var previousOrder = previousSameWarzoneOrders.FirstOrDefault(WasPurchasedWithinPurchaseCooldownSeconds);
                if (previousOrder != null)
                    throw new DomainException($"This UAV is under cooldown time. {previousOrder.ResolveUavCooldownText()}");
            }

            if (order.Player!.IsVip() && previousSameWarzoneOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.ScumServer.Uav.StockPerVipPlayer)
                throw new DomainException("This Warzone Teleport is out of stock, try again later.");

            if (!order.Player!.IsVip() && previousSameWarzoneOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.ScumServer.Uav.StockPerPlayer)
                throw new DomainException("This Warzone Teleport is out of stock, try again later.");

            if (order.ScumServer.Uav.IsBlockPurchaseRaidTime)
            {
                var raidTimes = _cacheService.GetRaidTimes(order.ScumServer.Id);
                if (raidTimes is not null && raidTimes.IsInRaidTime(order.ScumServer))
                    throw new DomainException("This Warzone Teleport cannot be purchased during Raid Period.");
            }
        }


        public async Task ValidateAsync(Order order)
        {
            if (order.Player is null)
                throw new DomainException("Player not yet registered, please register using the Welcome Pack.");

            _player = order.Player;

            if (!order.ScumServer.ShopEnabled)
                throw new DomainException("Shop is disabled at the moment.");

            var price = order.GetPrice();
            if (order.Player.Coin < price) throw new DomainException($"You don't have enough bot coins.\nYour current balance: {order.Player.Coin}\nPrice: {price}");

            if (order.OrderType == Shared.Enums.EOrderType.Pack)
                await ValidatePack(order);
            else if (order.OrderType == Shared.Enums.EOrderType.Warzone)
                await ValidateWarzone(order);
            else if (order.OrderType == Shared.Enums.EOrderType.UAV)
                await ValidateUav(order);
        }


        public bool WasPurchasedWithinLast24Hours(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalHours <= 24;
        }
    }
}
