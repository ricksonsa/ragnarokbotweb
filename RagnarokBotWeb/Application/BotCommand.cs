using Shared.Enums;

namespace RagnarokBotWeb.Application
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
        public List<BotCommandValue> Values { get; set; } = [];
        public BotCommand Extra { get; set; }
        public string Data { get; set; }

        public BotCommand() { }

        public void ListPlayers()
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ListPlayers
            });
        }

        public void Command(string command)
        {
            Values.Add(new BotCommandValue
            {
                Value = command,
                Type = ECommandType.Command
            });
        }

        public void Say(string command)
        {
            if (command.StartsWith("#")) command = command[1..];
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Say,
                Value = command,
            });
        }

        public void Teleport(string target, string coordinates)
        {
            Values.Add(new BotCommandValue
            {
                Target = target,
                Type = ECommandType.TeleportPlayer,
                Value = target,
                Coordinates = coordinates
            });
        }

        public void Delivery(string target, string value, int amount)
        {
            Values.Add(new BotCommandValue
            {
                Target = target,
                Type = ECommandType.Delivery,
                Value = value,
                Amount = amount,
            });
        }

        public void Delivery(string target, string value, int amount, BotCommand extra)
        {
            Extra = extra;
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Delivery,
                Target = target,
                Value = value,
                Amount = amount,
            });
        }

        public void Announce(string value)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Announce,
                Value = value
            });
        }
    }
}
