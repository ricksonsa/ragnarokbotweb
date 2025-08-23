using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class User : BaseEntity
    {
        public Tenant Tenant { get; set; }
        public string Name { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; private set; }
        public byte[] PasswordSalt { get; private set; }
        public bool Active { get; set; }
        public string? Country { get; set; }
        public string? FastspringAccountId { get; set; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Default;

        public User() { }

        public void SetPassword(string password)
        {
            PasswordHash = PasswordHasher.HashPassword(password, out byte[] salt);
            PasswordSalt = salt;
        }

        public bool IsTenantAvaiable()
        {
            return Tenant is not null && Tenant.Enabled;
        }
    }
}
