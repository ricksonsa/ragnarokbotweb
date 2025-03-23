namespace RagnarokBotWeb.Domain.Entities
{
    public class Bunker : BaseEntity
    {
        public string Sector { get; set; }
        public bool Locked { get; set; }
        public DateTime? Available { get; set; }
        public ScumServer ScumServer { get; set; }

        public Bunker() { }

        public Bunker(string sector)
        {
            Sector = sector;
            Locked = true;
        }
    }
}
