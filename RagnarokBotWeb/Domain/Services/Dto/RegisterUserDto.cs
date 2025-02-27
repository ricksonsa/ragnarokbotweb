namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class RegisterUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public long? TenantId { get; set; }
    }
}
