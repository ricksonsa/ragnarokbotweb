using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Exchange : BaseOrderEntity
    {
        public bool AllowWithdraw { get; set; }
        public bool AllowDeposit { get; set; }
        public bool AllowTransfer { get; set; }
        public double WithdrawRate { get; set; }
        public double DepositRate { get; set; }
        public double TransferRate { get; set; }
        public EExchangeGameCurrencyType CurrencyType { get; set; } = EExchangeGameCurrencyType.Money;
    }
}
