using Shared.Enums;

namespace RagnarokBotClient
{
    public class BotCommandValue
    {
        public string? Coordinates { get; set; }
        public string? Target { get; set; }
        public string Value { get; set; }
        public int Amount { get; set; } = 1;
        public ECommandType Type { get; set; }
    }

    public class BotCommand
    {
        public List<BotCommandValue> Values { get; set; }
        public BotCommand Extra { get; set; }
        public string Data { get; set; }
    }
}
