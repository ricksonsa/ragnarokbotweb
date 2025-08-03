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

        public BotCommand ListPlayers()
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ListPlayers
            });
            return this;
        }

        public BotCommand ListSquads()
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ListSquads
            });
            return this;
        }

        public BotCommand Kick(string steamId)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Kick,
                Value = steamId
            });
            return this;
        }

        public BotCommand Ban(string steamId)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Ban,
                Value = steamId
            });
            return this;
        }

        public BotCommand Command(string command)
        {
            Values.Add(new BotCommandValue
            {
                Value = command,
                Type = ECommandType.Command
            });
            return this;
        }

        public BotCommand Say(string command)
        {
            if (command.StartsWith("#")) command = command[1..];
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Say,
                Value = command,
            });
            return this;
        }

        public BotCommand Teleport(string target, string coordinates)
        {
            Values.Add(new BotCommandValue
            {
                Target = target,
                Type = ECommandType.TeleportPlayer,
                Value = target,
                Coordinates = coordinates
            });
            return this;
        }

        public BotCommand Delivery(string target, string value, int amount)
        {
            Values.Add(new BotCommandValue
            {
                Target = target,
                Type = ECommandType.SimpleDelivery,
                Value = value,
                Amount = amount,
            });
            return this;
        }

        public BotCommand MagazineDelivery(string coordinates, string item, int amount, int ammoCount)
        {
            Values.Add(new BotCommandValue
            {
                Target = item,
                Type = ECommandType.MagazineDelivery,
                Value = ammoCount.ToString(),
                Amount = amount,
                Coordinates = coordinates
            });
            return this;
        }

        public BotCommand Delivery(string target, string value, int amount, BotCommand extra)
        {
            Extra = extra;
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.SimpleDelivery,
                Target = target,
                Value = value,
                Amount = amount,
            });
            return this;
        }

        public BotCommand Announce(string value)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Announce,
                Value = value
            });
            return this;
        }
    }
}
