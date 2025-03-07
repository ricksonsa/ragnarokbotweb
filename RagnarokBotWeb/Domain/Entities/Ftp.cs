using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Ftp : BaseEntity
    {
        public EHostProvider Provider { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public long Port { get; set; }
        public string? RootFolder { get; set; }

        public Ftp() { }

        public Ftp(EHostProvider provider)
        {
            Provider = provider;
            RootFolder = GetRootFolder();
        }

        public string GetRootFolder() => Provider switch
        {
            EHostProvider.PingPerfect => "/" + Address + "_7000/",
            EHostProvider.GGHosting => "/" + Address + "_7000/",// FIXME: Resolver root folder gghosting
            _ => $"/",
        };

    }
}
