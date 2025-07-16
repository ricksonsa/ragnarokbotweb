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

        public async Task ValidateAsync(Order order)
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
            if (order.Pack.PurchaseCooldownSeconds.HasValue && previousSamePackOrders.Any(WasPurchasedWithinPurchaseCooldownSeconds))
            {
                throw new DomainException("The purchase for this pack is under cooldown time, it will be available soon.");
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

        public long Charge(Order order)
        {
            var price = order.Player!.IsVip() ? Convert.ToInt64(order.Pack!.VipPrice) : Convert.ToInt64(order.Pack!.Price);
            order.Player!.Coin -= price;
            return order.Player!.Coin;
        }

        private bool WasPurchasedWithinPurchaseCooldownSeconds(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.Pack!.PurchaseCooldownSeconds;
        }

        public bool WasPurchasedWithinLast24Hours(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalHours <= 24;
        }
    }
}
