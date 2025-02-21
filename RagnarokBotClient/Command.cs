using Shared.Enums;

namespace RagnarokBotClient
{
    public class Command
    {
        public string? Coordinates { get; set; }
        public string? Target { get; set; }
        public string Value { get; set; }
        public ECommandType Type { get; set; }
        public int Amount { get; set; } = 1;
    }
}
