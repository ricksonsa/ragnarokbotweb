using RagnarokBotWeb.Application.Security;

namespace RagnarokBotWeb.Domain.Entities
{
    public class User : BaseEntity
    {
        public Tenant? Tenant { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; private set; }
        public byte[] PasswordSalt { get; private set; }
        public DateTime CreateDate { get; set; }
        public bool Active { get; set; }

        public User()
        {
            CreateDate = DateTime.Now;
        }

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
