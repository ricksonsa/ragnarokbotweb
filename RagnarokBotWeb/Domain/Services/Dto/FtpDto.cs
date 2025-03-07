using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class FtpDto
    {
        public EHostProvider Provider { get; set; }
        public string Address { get; set; }
        public long Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
