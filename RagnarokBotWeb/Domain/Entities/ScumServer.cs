namespace RagnarokBotWeb.Domain.Entities
{
    public class ScumServer : BaseEntity
    {
        public Tenant Tenant { get; set; }
        public Guild? Guild { get; set; }
        public Ftp? Ftp { get; set; }

        public ScumServer(Tenant tenant)
        {
            Tenant = tenant;
        }
    }
}
