namespace RagnarokBotWeb.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string? Name { get; set; }
        public bool Enabled { get; set; }
    }
}
