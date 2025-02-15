using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Command
    {
        public string? Coordinates { get; set; }
        public string? Target { get; set; }
        public string Value { get; set; }
        public ECommandType Type { get; set; }
        public int Amount { get; set; } = 1;
        public Bot? Bot { get; set; }

        public Command() { }
    }
}
