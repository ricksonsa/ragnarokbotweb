namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class UserDto
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string? Country { get; set; }
    }
}
