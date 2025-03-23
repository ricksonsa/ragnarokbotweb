namespace RagnarokBotWeb.Domain.Entities
{
    public class Kill : BaseEntity
    {
        public string? KillerSteamId64 { get; set; }
        public string? TargetSteamId64 { get; set; }
        public string? KillerName { get; set; }
        public string? TargetName { get; set; }
        public string? Weapon { get; set; }
        public float? Distance { get; set; }
        public string? Sector { get; set; }
        public float KillerX { get; set; }
        public float KillerY { get; set; }
        public float KillerZ { get; set; }

        public float VictimX { get; set; }
        public float VictimY { get; set; }
        public float VictimZ { get; set; }
        public ScumServer ScumServer { get; set; }
    }
}
