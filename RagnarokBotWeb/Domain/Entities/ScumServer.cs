using System.ComponentModel.DataAnnotations.Schema;

namespace RagnarokBotWeb.Domain.Entities
{
    public class ScumServer : BaseEntity
    {
        public Tenant Tenant { get; set; }
        [ForeignKey("GuildId")]
        public Guild? Guild { get; set; }
        public Ftp? Ftp { get; set; }

        public ScumServer(Tenant tenant)
        {
            Tenant = tenant;
        }

        public ScumServer()
        {
            
        }
    }
}
