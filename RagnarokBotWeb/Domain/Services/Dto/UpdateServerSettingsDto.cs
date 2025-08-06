namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class UpdateServerSettingsDto
    {
        public long CoinAwardPeriodically { get; set; }
        public long VipCoinAwardPeriodically { get; set; }
        public string? BattleMetricsId { get; set; }
        public bool AllowMinesOutsideFlag { get; set; } = true;
        public bool AnnounceMineOutsideFlag { get; set; } = true;
        public long CoinReductionPerInvalidMineKill { get; set; }
        public bool SendVipLockpickAlert { get; set; }
        public bool ShopEnabled { get; set; } = true;
        public long CoinAwardIntervalMinutes { get; set; }
        public long CoinDeathPenaltyAmount { get; set; }
        public long CoinKillAwardAmount { get; set; }
        public long WelcomePackCoinAward { get; set; }
        public List<string> RestartTimes { get; set; }
    }
}
