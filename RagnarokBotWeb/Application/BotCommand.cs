using Shared.Enums;

namespace RagnarokBotWeb.Application
{
    public class BotCommand
    {
        public string? Coordinates { get; set; }
        public string? Target { get; set; }
        public string Value { get; set; }
        public ECommandType Type { get; set; }
        public int Amount { get; set; } = 1;

        public BotCommand() { }

        public static BotCommand ListPlayers()
        {
            return new BotCommand
            {
                Type = ECommandType.ListPlayers
            };
        }
    }
}
