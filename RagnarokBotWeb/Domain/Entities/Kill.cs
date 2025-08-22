
using RagnarokBotWeb.Domain.Entities.Base;

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
                if (string.IsNullOrEmpty(Weapon) || string.IsNullOrWhiteSpace(Weapon)) return "Unknown";
                var resolved = Weapon.Contains("_C") ? Weapon.Substring(0, Weapon.LastIndexOf("_C")) : Weapon;
                return resolved.Replace("Weapon_", string.Empty)
                    .Replace("1H_", string.Empty)
                    .Replace("2H_", string.Empty)
                    .Replace("_", " ");
            }
        }
        public float? Distance { get; set; }
        public string? Sector { get; set; }
        public double KillerX { get; set; }
        public double KillerY { get; set; }
        public double KillerZ { get; set; }

        public double VictimX { get; set; }
        public double VictimY { get; set; }
        public double VictimZ { get; set; }
        public ScumServer ScumServer { get; set; }
        public string? ImageUrl { get; set; }
        public string? KillHash { get; set; }
        public bool IsSameSquad { get; set; }


        public void SetHash()
        {
            KillHash = Math.Abs($"{KillerSteamId64}{TargetSteamId64}{Weapon}{KillerX}{KillerY}{KillerZ}{VictimX}{VictimY}{VictimZ}".GetHashCode()).ToString();
        }

        public static bool IsMine(string? weapon)
        {
            if (string.IsNullOrEmpty(weapon)) return false;
            weapon = weapon.ToLower();
            return weapon.Contains("trap") || weapon.Contains("mine") || weapon.Contains("claymore");
        }

    }
}
