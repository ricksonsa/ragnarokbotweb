using RagnarokBotWeb.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities
{
    public class ScumServer : BaseEntity
    {
        public string? Name { get; set; }
        public int? Slots { get; set; }
        public string? BattleMetricsId { get; set; }
        public Tenant Tenant { get; set; }
        [ForeignKey("GuildId")]
        public Guild? Guild { get; set; }
        public Ftp? Ftp { get; set; }
        public long? UavId { get; set; }
        public Uav Uav { get; set; }
        public long? ExchangeId { get; set; }
        public Exchange Exchange { get; set; }
        public string? RestartTimes { get; private set; }
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
        public bool ShowKillOnMap { get; set; } = false;
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

        public ScumServer(Tenant tenant)
        {
            Tenant = tenant;
            KillAnnounceText = "{killer_name} killed {victim_name} with {weapon} at a distance of {distance} sector {sector}";
        }

        public ScumServer()
        {
            KillAnnounceText = "{killer_name} killed {victim_name} with {weapon} at a distance of {distance} sector {sector}";
        }

        public void SetRestartTimes(List<string> restartTimes)
        {
            RestartTimes = string.Join(";", restartTimes);
        }

        public List<string> GetRestartTimesList()
        {
            return string.IsNullOrEmpty(RestartTimes) ? new List<string>() : RestartTimes.Split(";").ToList();
        }

        public TimeZoneInfo GetTimeZoneOrDefault()
        {
            return TimeZoneId == null ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }

        public bool IsCompliant()
        {
            return Tenant?.IsCompliant() ?? false;
        }
    }
}
