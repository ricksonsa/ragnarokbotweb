using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class AccountDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string LastName { get; set; }
        public long ServerId { get; set; }
        public string? Country { get; set; }
        public IEnumerable<ScumServerDto> Servers { get; set; }
        public ScumServerDto? Server { get; set; }
        public AccessLevel AccessLevel { get; set; }
    }
}
