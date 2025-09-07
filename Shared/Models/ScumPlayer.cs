namespace Shared.Models
{
    public class ScumPlayer
    {
        public string Name { get; set; }
        public string SteamName { get; set; }
        public string SteamID { get; set; }
        public long Fame { get; set; }
        public long AccountBalance { get; set; }
        public long GoldBalance { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string? SquadName { get; set; }
        public int? SquadId { get; set; }

        public override string ToString()
        {
            return $"{Name} (Steam: {SteamName}, ID: {SteamID}) - Fame: {Fame}, Balance: {AccountBalance} Gold: {GoldBalance}, Location: ({X}, {Y}, {Z})";
        }
    }
}
