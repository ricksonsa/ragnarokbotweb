namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class AccountDto
    {
        public string Email { get; set; }
        public long ServerId { get; set; }
        public IEnumerable<ScumServerDto> Servers { get; set; }
        public ScumServerDto? Server { get; set; }
    }
}
