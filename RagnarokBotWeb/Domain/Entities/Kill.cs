namespace RagnarokBotWeb.Domain.Entities
{
    public class Kill : BaseEntity
    {
        public User? Killer { get; set; }
        public User? Target { get; set; }
        public string? KillerSteamId64 { get; set; }
        public string? TargetSteamId64 { get; set; }
        public string? KillerName { get; set; }
        public string? TargetName { get; set; }
        public string? Weapon { get; set; }
        public float? Distance { get; set; }
        public string? Sector { get; set; }
        public DateTime CreateDate { get; set; }

        public Kill()
        {
            CreateDate = DateTime.Now;
        }
    }
}
