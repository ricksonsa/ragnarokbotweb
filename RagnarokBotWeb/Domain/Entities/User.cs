namespace RagnarokBotWeb.Domain.Entities
{
    public class User : BaseEntity
    {
        public Tenant Tenant { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreateDate { get; set; }
        public bool Active { get; set; }

        public User()
        {
            CreateDate = DateTime.Now;
        }
    }
}
