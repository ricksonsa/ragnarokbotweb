using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities
{
    public class ScumServer : BaseEntity
    {
        public string? Name { get; set; }
        public Tenant Tenant { get; set; }
        [ForeignKey("GuildId")]
        public Guild? Guild { get; set; }
        public Ftp? Ftp { get; set; }
        public string? RestartTimes { get; private set; }
        public string? TimeZoneId { get; set; }

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
        public ScumServer(Tenant tenant)
        {
            Tenant = tenant;
        }

        public ScumServer() { }

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
    }
}
