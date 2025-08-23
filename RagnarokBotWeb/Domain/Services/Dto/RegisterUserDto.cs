namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class RegisterUserDto
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public long? TenantId { get; set; }
        public string? Country { get; set; }
    }
}
