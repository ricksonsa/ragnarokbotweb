namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class ScumServerDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public FtpDto Ftp { get; set; }
        public List<string> RestartTimes { get; set; }
        public DiscordDto? Discord { get; set; }

        #region Kill Feed
        public bool UseKillFeed { get; set; }
        public bool ShowKillDistance { get; set; }
        public bool ShowKillSector { get; set; }
        public bool ShowKillWeapon { get; set; }
        public bool HideKillerName { get; set; }
        public bool HideMineKill { get; set; }
        public bool ShowSameSquadKill { get; set; }
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
        public long CoinAwardPeriodically { get; set; }
        public long VipCoinAwardPeriodically { get; set; }
        #endregion
    }
}
