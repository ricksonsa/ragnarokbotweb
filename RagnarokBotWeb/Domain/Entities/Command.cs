using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Command : BaseEntity
    {
        public Guid Uuid { get; set; }
        public string? Coordinates { get; set; }
        public string? Target { get; set; }
        public string? Value { get; set; }
        public bool Completed { get; set; } = false;
        public ECommandType Type { get; set; }
        public Bot? Bot { get; set; }

        public Command()
        {
            Uuid = Guid.NewGuid();
        }
    }
}
