using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Domain.Business
{
    public class OrderPurchaseProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICacheService _cacheService;

        private Player _player;
        private Player Player { get => _player; set => _player = value; }
        private BaseOrderEntity _item;
        private BaseOrderEntity Item { get => _item; set => _item = value; }

        public OrderPurchaseProcessor(IOrderRepository orderRepository, ICacheService cacheService)
        {
            _orderRepository = orderRepository;
            _cacheService = cacheService;
        }

        private bool WasPurchasedWithinPurchaseCooldownSeconds(Order order)
        {
            var item = order.GetItem();
            return Item.Id == item.Id && (DateTime.UtcNow - order.CreateDate).TotalSeconds <= order.GetItem().PurchaseCooldownSeconds;
        }

        private async Task ValidateOrderItem(Order order, bool validatePrice = true)
        {
            if (validatePrice)
            {
                var price = order.GetPrice();
                if (Player.Coin < price) throw new DomainException($"You don't have enough coins.\nYour current balance: {Player.Coin}\nPrice: {price}");
            }

            var item = order.GetItem();

            if (!item.Enabled)
                throw new DomainException("This shop item is not available at the moment.");

            if (item.IsVipOnly && !Player.IsVip())
                throw new DomainException("This shop item is only available for Vip Players.");

            if (item.IsBlockPurchaseRaidTime)
            {
                var raidTimes = _cacheService.GetRaidTimes(order.ScumServer.Id);
                if (raidTimes is not null && raidTimes.IsInRaidTime(order.ScumServer))
                    throw new DomainException("This shop item cannot be purchased during Raid Period.");
            }

            List<Order> previousSameOrders = await GetLastSameOrders(order);
            if (item.PurchaseCooldownSeconds.HasValue)
            {
                var previousOrder = previousSameOrders.FirstOrDefault(WasPurchasedWithinPurchaseCooldownSeconds);
                if (previousOrder != null)
                    throw new DomainException($"This shop item is under cooldown time. {previousOrder.ResolveCooldownText(item)}");
            }

            var maxStock = order.Player!.IsVip() ? item.StockPerVipPlayer : item.StockPerPlayer;

            if (previousSameOrders.Where(WasPurchasedWithinLast24Hours).Count() >= maxStock)
                throw new DomainException("This shop item is out of stock, try again later.");
        }

        private async Task<List<Order>> GetLastSameOrders(Order order)
        {
            if (order.OrderType == EOrderType.Pack)
            {
                return await _orderRepository.FindManyForProcessor(o =>
                o.ScumServer.Id == order.ScumServer.Id
                && o.OrderType == order.OrderType
                && o.Pack!.Id == order.Pack!.Id
                && order.Player != null
                && order.Player.Id == _player.Id);
            }
            else if (order.OrderType == EOrderType.Taxi)
            {
                return await _orderRepository.FindManyForProcessor(o =>
                o.ScumServer.Id == order.ScumServer.Id
                && o.OrderType == order.OrderType
                && o.Taxi!.Id == order.Taxi!.Id
                && order.Player != null
                && order.Player.Id == _player.Id);
            }
            else if (order.OrderType == EOrderType.Warzone)
            {
                return await _orderRepository.FindManyForProcessor(o =>
                o.ScumServer.Id == order.ScumServer.Id
                && o.OrderType == order.OrderType
                && o.Warzone!.Id == order.Warzone!.Id
                && order.Player != null
                && order.Player.Id == _player.Id);
            }

            return await _orderRepository.FindManyForProcessor(o =>
                o.ScumServer.Id == order.ScumServer.Id
                && o.OrderType == order.OrderType
                && order.Player != null
                && order.Player.Id == _player.Id);
        }

        private bool WasPurchasedWithinLast24Hours(Order order)
        {
            return (DateTime.UtcNow - order.CreateDate).TotalHours <= 24;
        }

        public async Task ValidateAsync(Order order)
        {
            if (!order.ScumServer.IsCompliant()) throw new DomainException("This feature is not available at the moment.");

            if (order.Player is null)
                throw new DomainException("Player not yet registered, please register using the Welcome Pack.");

            Player = order.Player;
            Item = order.GetItem();

            if (!order.ScumServer.ShopEnabled)
                throw new DomainException("Shop is disabled at the moment.");

            await ValidateOrderItem(order);
        }
    }
}
