namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class ScumServerDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? BattleMetricsId { get; set; }
        public FtpDto Ftp { get; set; }
        public List<string> RestartTimes { get; set; }
        public DiscordDto? Discord { get; set; }
        public UavDto? Uav { get; set; }
        public ExchangeDto? Exchange { get; set; }
        public bool IsCompliant { get; set; }
        public int Slots { get; set; }
        public string? TimeZoneId { get; set; }

        #region Kill Feed
        public bool UseKillFeed { get; set; } = true;
        public bool ShowKillDistance { get; set; } = true;
        public bool ShowKillSector { get; set; } = true;
        public bool ShowKillWeapon { get; set; } = true;
        public bool ShowKillerName { get; set; } = true;
        public bool ShowMineKill { get; set; } = true;
        public bool ShowSameSquadKill { get; set; } = true;
        public bool ShowKillCoordinates { get; set; } = true;
        public bool ShowKillOnMap { get; set; } = true;
        public string? KillAnnounceText { get; set; }
        #endregion

        #region Lockpick Feed
        public bool UseLockpickFeed { get; set; }
        public bool ShowLockpickSector { get; set; }
        public bool ShowLockpickContainerName { get; set; }
        public bool SendVipLockpickAlert { get; set; }

        #endregion

        #region Discord
        public bool SendLocalChatToDiscord { get; set; } = true;
        public bool SendGlobalChatToDiscord { get; set; } = true;
        #endregion

        #region Shop
        public bool ShopEnabled { get; set; } = true;
        public long CoinAwardIntervalMinutes { get; set; }
        public long CoinAwardPeriodically { get; set; }
        public long VipCoinAwardPeriodically { get; set; }
        public long CoinDeathPenaltyAmount { get; set; }
        public long CoinKillAwardAmount { get; set; }
        public long WelcomePackCoinAward { get; set; }
        #endregion

        #region Trap
        public bool AllowMinesOutsideFlag { get; set; } = true;
        public bool AnnounceMineOutsideFlag { get; set; } = true;
        public long CoinReductionPerInvalidMineKill { get; set; }
        #endregion

        #region Ranks
        public long? KillRankMonthlyTop1Award { get; set; }
        public long? KillRankMonthlyTop2Award { get; set; }
        public long? KillRankMonthlyTop3Award { get; set; }
        public long? KillRankMonthlyTop4Award { get; set; }
        public long? KillRankMonthlyTop5Award { get; set; }

        public long? KillRankWeeklyTop1Award { get; set; }
        public long? KillRankWeeklyTop2Award { get; set; }
        public long? KillRankWeeklyTop3Award { get; set; }
        public long? KillRankWeeklyTop4Award { get; set; }
        public long? KillRankWeeklyTop5Award { get; set; }

        public long? KillRankDailyTop1Award { get; set; }
        public long? KillRankDailyTop2Award { get; set; }
        public long? KillRankDailyTop3Award { get; set; }
        public long? KillRankDailyTop4Award { get; set; }
        public long? KillRankDailyTop5Award { get; set; }

        public long? LockpickRankDailyTop1Award { get; set; }
        public long? LockpickRankDailyTop2Award { get; set; }
        public long? LockpickRankDailyTop3Award { get; set; }
        public long? LockpickRankDailyTop4Award { get; set; }
        public long? LockpickRankDailyTop5Award { get; set; }
        #endregion
    }
}
