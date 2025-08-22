using RagnarokBotWeb.Domain.Entities.Base;
using Shared.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Pack? Pack { get; set; }
        public Warzone? Warzone { get; set; }
        public EOrderStatus Status { get; set; }
        public EOrderType OrderType { get; set; }
        public Player? Player { get; set; }
        public ScumServer ScumServer { get; set; }
        public string? Uav { get; set; }
        public string? TaxiTeleportId { get; set; }
        public Taxi? Taxi { get; set; }

        public long ResolvedPrice
        {
            get
            {
                long price = 0;
                long vipPrice = 0;
                switch (OrderType)
                {
                    case EOrderType.Pack:
                        price = Pack!.Price;
                        vipPrice = Pack!.VipPrice;
                        break;
                    case EOrderType.Warzone:
                        price = Warzone!.Price;
                        vipPrice = Warzone!.VipPrice;
                        break;
                    case EOrderType.UAV:
                        price = ScumServer.Uav!.Price;
                        vipPrice = ScumServer.Uav!.VipPrice;
                        break;
                    default: break;
                }
                if (Player is null) return price;
                return Player.IsVip() ? vipPrice : price;
            }

        }

        public long BalancePreview
        {
            get
            {
                if (Player is null) return 0;
                return Player.Coin - ResolvedPrice;
            }
        }

        public Order()
        {
            Status = EOrderStatus.Created;
        }

        private string ResolvePurchaseCooldownText(long seconds)
        {
            var now = DateTime.UtcNow;
            var remainingHours = Math.Round((CreateDate.AddSeconds(seconds) - now).TotalHours);
            var remainingMinutes = Math.Round((CreateDate.AddSeconds(seconds) - now).TotalMinutes);
            var remainingSeconds = Math.Round((CreateDate.AddSeconds(seconds) - now).TotalSeconds);

            if (remainingHours > 0) return $"\nNext purchase will be available in {Math.Round(remainingHours)} hours.";
            else if (remainingMinutes > 0) return $"\nNext purchase will be available in {Math.Round(remainingMinutes)} minutes.";
            else return $"\nNext purchase will be available in {Math.Round(remainingSeconds)} seconds.";
        }

        public string ResolveCooldownText(BaseOrderEntity orderItem)
        {
            if (orderItem.PurchaseCooldownSeconds.HasValue && orderItem.PurchaseCooldownSeconds.Value > 0)
            {
                return ResolvePurchaseCooldownText(orderItem.PurchaseCooldownSeconds.Value);
            }
            return string.Empty;
        }

        public long GetPrice()
        {
            if (Player is null) throw new Exception("Invalid player");
            var item = GetItem();
            return Player.IsVip() ? item!.VipPrice : item!.Price;
        }

        public string? ResolvedDeliveryText()
        {
            if (OrderType == EOrderType.Pack)
            {
                if (Pack == null || Pack.DeliveryText is null) return null;

                return Pack.DeliveryText
                    .Replace("{playerName}", Player?.Name)
                    .Replace("{packageName}", Pack?.Name)
                    .Replace("{orderId}", Id.ToString());
            }
            else if (OrderType == EOrderType.UAV)
            {
                if (Uav == null || ScumServer.Uav.DeliveryText is null) return null;

                return ScumServer.Uav.DeliveryText
                    .Replace("{sector}", Uav);
            }
            else
            {
                return null;
            }

        }

        public BaseOrderEntity GetItem()
        {
            switch (OrderType)
            {
                case EOrderType.Warzone: return Warzone!;
                case EOrderType.Pack: return Pack!;
                case EOrderType.UAV: return ScumServer.Uav!;
                case EOrderType.Exchange: return ScumServer.Exchange!;
                case EOrderType.Taxi: return Taxi!;
                default: throw new Exception("Invalid order");
            }
        }
    }
}
