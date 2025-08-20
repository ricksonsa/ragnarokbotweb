namespace RagnarokBotWeb.Domain.Entities
{
    public class Lockpick : BaseEntity
    {
        public string LockType { get; set; }
        public string SteamId64 { get; set; }
        public string TargetObject { get; set; }
        public long ScumId { get; set; }
        public string Name { get; set; }
        public DateTime AttemptDate { get; set; }
        public bool Success { get; set; }
        public int Attempts { get; set; }
        public ScumServer ScumServer { get; set; }

    }
}
