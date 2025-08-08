namespace RagnarokBotWeb.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string? Name { get; set; }
        public ulong? DiscordId { get; set; }
        public string? Email { get; set; }
        public bool Enabled { get; set; }
        public IEnumerable<ScumServer> ScumServers { get; set; }
        public IEnumerable<Payment> Payments { get; set; }
    }
}
