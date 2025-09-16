using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class ExchangeDto
    {
        public long Id { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? DiscordChannelId { get; set; }
        public ulong? DiscordMessageId { get; set; }
        public long? PurchaseCooldownSeconds { get; set; }
        public long? StockPerPlayer { get; set; }
        public long? StockPerVipPlayer { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool IsVipOnly { get; set; }
        public string? ImageUrl { get; set; }
        public bool AllowWithdraw { get; set; }
        public bool AllowDeposit { get; set; }
        public bool AllowTransfer { get; set; }
        public double WithdrawRate { get; set; }
        public double DepositRate { get; set; }
        public double TransferRate { get; set; }
        public EExchangeGameCurrencyType CurrencyType { get; set; } = EExchangeGameCurrencyType.Money;
    }
}
