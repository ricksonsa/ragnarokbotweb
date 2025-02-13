namespace RagnarokBotWeb.Domain.Entities
{
    public class Item : BaseEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public bool Active { get; set; }
    }
}
