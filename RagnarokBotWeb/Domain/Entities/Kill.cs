namespace RagnarokBotWeb.Domain.Entities
{
    public class Kill : BaseEntity
    {
        public string? KillerSteamId64 { get; set; }
        public string? TargetSteamId64 { get; set; }
        public string? KillerName { get; set; }
        public string? TargetName { get; set; }
        public string? Weapon { get; set; }
        public string DisplayWeapon
        {
            get
            {
                if (Weapon is null) return "";
                var resolved = Weapon.Substring(0, Weapon.LastIndexOf("_C"));
                return resolved.Replace("Weapon_", string.Empty)
                    .Replace("1H_", string.Empty)
                    .Replace("2H_", string.Empty)
                    .Replace("_", " ");
            }
        }
        public float? Distance { get; set; }
        public string? Sector { get; set; }
        public float KillerX { get; set; }
        public float KillerY { get; set; }
        public float KillerZ { get; set; }

        public float VictimX { get; set; }
        public float VictimY { get; set; }
        public float VictimZ { get; set; }
        public ScumServer ScumServer { get; set; }
        public string? ImageUrl { get; set; }
    }
}
