namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class UpdateKillFeedDto
    {
        public bool UseKillFeed { get; set; }
        public bool ShowKillDistance { get; set; }
        public bool ShowKillSector { get; set; }
        public bool ShowKillWeapon { get; set; }
        public bool ShowKillerName { get; set; }
        public bool ShowMineKill { get; set; }
        public bool ShowSameSquadKill { get; set; }
        public bool ShowKillCoordinates { get; set; }
        public bool ShowKillOnMap { get; set; }
        public string? KillAnnounceText { get; set; }
    }
}
