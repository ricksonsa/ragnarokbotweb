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

        public static BotCommand Command(string command)
        {
            return new BotCommand
            {
                Value = command,
                Type = ECommandType.Command
            };
        }

        public static BotCommand Delivery(string target, string value, int amount)
        {
            return new BotCommand
            {
                Target = target,
                Value = value,
                Amount = amount,
                Type = ECommandType.Delivery
            };
        }
    }
}
