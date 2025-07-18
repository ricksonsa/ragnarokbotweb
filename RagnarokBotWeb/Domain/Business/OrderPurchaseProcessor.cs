using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Business
{
    public class OrderPurchaseProcessor
    {
        private readonly IOrderRepository _orderRepository;

        public OrderPurchaseProcessor(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
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
                    throw new DomainException($"This pack is under cooldown time. {previousOrder.ResolveWarzoneCooldownText()}");
                }
            }

            if (previousSamePackOrders.Where(WasPurchasedWithinLast24Hours).Count() >= order.Pack.StockPerPlayer)
            {
                throw new DomainException("This pack is out of stock, try again later.");
            }

            if (order.Pack.IsBlockPurchaseRaidTime)
            {
                // TODO: Get server raid time and throw error
            }

            var price = order.Player!.IsVip() ? Convert.ToInt64(order.Pack!.VipPrice) : Convert.ToInt64(order.Pack!.Price);
            if (order.Player.Coin < price) throw new DomainException("Player does not have enough bot coin");
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
                // TODO: Get server raid time and throw error
            }

            var price = order.Player!.IsVip() ? Convert.ToInt64(order.Warzone!.VipPrice) : Convert.ToInt64(order.Warzone!.Price);
            if (order.Player.Coin < price) throw new DomainException("User does not have enough bot coin.");
        }

        public async Task ValidateAsync(Order order)
        {
            if (order.OrderType == Shared.Enums.EOrderType.Pack)
            {
                await ValidatePack(order);
            }
            else if (order.OrderType == Shared.Enums.EOrderType.Warzone)
            {
                await ValidateWarzone(order);

            }
        }

        public long Charge(Order order)
        {
            var price = order.Player!.IsVip() ? Convert.ToInt64(order.Pack!.VipPrice) : Convert.ToInt64(order.Pack!.Price);
            order.Player!.Coin -= price;
            return order.Player!.Coin;
        }

        private bool WasPurchasedWithinPurchaseCooldownSeconds(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.Warzone!.PurchaseCooldownSeconds;
        }

        public bool WasPurchasedWithinLast24Hours(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalHours <= 24;
        }
    }
}
