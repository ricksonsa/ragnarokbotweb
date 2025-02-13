namespace RagnarokBotWeb.Domain.Entities
{
    public class Kill : BaseEntity
    {
        public User Killer { get; set; }
        public User Target { get; set; }
        public string Weapon { get; set; }
        public float Distance { get; set; }
        public string? Sector { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
