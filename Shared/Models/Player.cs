namespace Shared.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string SteamName { get; set; }
        public string SteamID { get; set; }
        public long Fame { get; set; }
        public long AccountBalance { get; set; }
        public long GoldBalance { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override string ToString()
        {
            return $"{Name} (Steam: {SteamName}, ID: {SteamID}) - Fame: {Fame}, Balance: {AccountBalance} Gold: {GoldBalance}, Location: ({X}, {Y}, {Z})";
        }
    }
}
