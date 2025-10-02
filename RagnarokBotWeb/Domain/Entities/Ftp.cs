using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Ftp : BaseEntity
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public long Port { get; set; }
        public string? RootFolder { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
