namespace Shared.Models
{
    public class ScumServer
    {
        public long Id { get; set; }
        public string Name { get; set; }

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
    }
}
