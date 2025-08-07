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
            var remainingMinutes = (CreateDate.AddSeconds(seconds) - now).TotalMinutes;
            var remainingSeconds = (CreateDate.AddSeconds(seconds) - now).TotalSeconds;

            if (Math.Round(remainingMinutes) > 0) return $"\nNext purchase will be available in {Math.Round(remainingMinutes)} minutes.";
            return $"\nNext purchase will be available in {Math.Round(remainingSeconds)} seconds.";
        }

        public string ResolveUavCooldownText()
        {
            if (ScumServer.Uav.PurchaseCooldownSeconds.HasValue && ScumServer.Uav.PurchaseCooldownSeconds.Value > 0)
            {
                return ResolvePurchaseCooldownText(ScumServer.Uav.PurchaseCooldownSeconds.Value);
            }
            return string.Empty;
        }

        public string ResolveWarzoneCooldownText()
        {
            if (Warzone!.PurchaseCooldownSeconds.HasValue && Warzone.PurchaseCooldownSeconds.Value > 0)
            {
                return ResolvePurchaseCooldownText(Warzone.PurchaseCooldownSeconds.Value);
            }
            return string.Empty;
        }

        public string ResolvePackCooldownText()
        {
            if (Pack!.PurchaseCooldownSeconds.HasValue && Pack.PurchaseCooldownSeconds.Value > 0)
            {
                return ResolvePurchaseCooldownText(Pack.PurchaseCooldownSeconds.Value);
            }
            return string.Empty;
        }

        public long GetPrice()
        {
            if (Player is null) throw new Exception("Invalid player");
            switch (OrderType)
            {
                case EOrderType.Warzone:
                    return Player.IsVip() ? Warzone!.VipPrice : Warzone!.Price;
                case EOrderType.Pack:
                    return Player.IsVip() ? Pack!.VipPrice : Pack!.Price;
                case EOrderType.UAV:
                    return Player.IsVip() ? ScumServer.Uav.VipPrice : ScumServer.Uav.Price;
                default:
                    throw new Exception("Invalid order");
            }
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
    }
}
