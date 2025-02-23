namespace RagnarokBotWeb.Domain.Entities
{
    public class Command : BaseEntity
    {
        public string Value { get; set; }
        public Bot? Bot { get; set; }
        public bool Executed { get; set; }
        public DateTime? ExecuteDate { get; set; }
    }
}
