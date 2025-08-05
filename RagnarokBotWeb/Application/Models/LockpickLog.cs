namespace RagnarokBotWeb.Application.Models
{
    public class LockpickLog
    {
        public string User { get; set; }
        public int ScumId { get; set; }
        public string SteamId { get; set; }
        public bool Success { get; set; }
        public float ElapsedTime { get; set; }
        public int FailedAttempts { get; set; }
        public string TargetObject { get; set; }
        public string TargetId { get; set; }
        public string LockType { get; set; }
        public int OwnerScumId { get; set; }
        public string OwnerSteamId { get; set; }
        public string OwnerName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public DateTime? Date { get; set; }
        public string Line { get; set; }
        public string DisplayLockType
        {
            get
            {
                switch (LockType.ToLower())
                {
                    case "diallock": return "Dial Lock";
                    case "basic": return "Iron Lock";
                    case "medium": return "Silver Lock";
                    case "advanced": return "Gold Lock";
                    default: return "Unknown";
                }
            }
        }
    }
}
