using MessagePack;
using Shared.Enums;

namespace Shared.Models
{
    [MessagePackObject]
    public class BotCommandValue
    {
        [Key(0)] public string? Coordinates { get; set; }
        [Key(1)] public string? Target { get; set; }
        [Key(2)] public string Value { get; set; }
        [Key(3)] public int Amount { get; set; } = 1;
        [Key(4)] public ECommandType Type { get; set; }
        [Key(5)] public bool CheckTargetOnline { get; set; }
        public BotCommandValue() { }
        public BotCommandValue(bool checkTargetOnline) => CheckTargetOnline = checkTargetOnline;
    }

    [MessagePackObject]
    public class BotCommand
    {
        [Key(0)] public List<BotCommandValue> Values { get; set; } = [];
        [Key(1)] public string Data { get; set; }

        public BotCommand() { }

        public BotCommand SayOrCommand(string value)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.SayOrCommand,
                Value = value
            });
            return this;
        }

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

        public BotCommand ListFlags()
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ListFlags
            });
            return this;
        }

        public BotCommand Silence(string steamId, bool checkTargetOnline = true)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Silence,
                Value = steamId,
                CheckTargetOnline = checkTargetOnline
            });
            return this;
        }

        public BotCommand Kick(string steamId, bool checkTargetOnline = true)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Kick,
                Value = steamId,
                CheckTargetOnline = checkTargetOnline
            });
            return this;
        }

        public BotCommand Ban(string steamId, bool checkTargetOnline = true)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Ban,
                Value = steamId,
                CheckTargetOnline = checkTargetOnline
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

        public BotCommand Reconnect()
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.Reconnect
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

        public BotCommand Teleport(string target, string coordinates, bool checkTargetOnline = true)
        {
            Values.Add(new BotCommandValue
            {
                Target = target,
                Type = ECommandType.TeleportPlayer,
                Value = target,
                Coordinates = coordinates,
                CheckTargetOnline = checkTargetOnline
            });
            return this;
        }

        public BotCommand Delivery(string target, string value, int amount, bool checkTargetOnline = true)
        {
            Values.Add(new BotCommandValue
            {
                Target = target,
                Type = ECommandType.SimpleDelivery,
                Value = value,
                Amount = amount,
                CheckTargetOnline = checkTargetOnline
            });
            return this;
        }

        public BotCommand MagazineDelivery(string coordinates, string item, int amount, int ammoCount, bool checkTargetOnline = true)
        {
            Values.Add(new BotCommandValue
            {
                Target = item,
                Type = ECommandType.MagazineDelivery,
                Value = ammoCount.ToString(),
                Amount = amount,
                Coordinates = coordinates,
                CheckTargetOnline = checkTargetOnline
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

        public BotCommand ChangeFame(string target, long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeFame,
                Target = target,
                Value = amount.ToString(),
                CheckTargetOnline = false
            });
            return this;
        }

        public BotCommand ChangeGold(string target, long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeGold,
                Target = target,
                Value = amount.ToString(),
                CheckTargetOnline = true
            });
            return this;
        }

        public BotCommand ChangeMoney(string target, long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeMoney,
                Target = target,
                Value = amount.ToString(),
                CheckTargetOnline = true
            });
            return this;
        }

        public BotCommand ChangeGoldToAll(long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeGoldToAll,
                Value = amount.ToString(),
                CheckTargetOnline = false
            });
            return this;
        }

        public BotCommand ChangeMoneyToAll(long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeMoneyToAll,
                Value = amount.ToString(),
                CheckTargetOnline = false
            });
            return this;
        }

        public BotCommand ChangeGoldToAllOnline(long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeGoldToAllOnline,
                Value = amount.ToString(),
                CheckTargetOnline = false
            });
            return this;
        }

        public BotCommand ChangeMoneyToAllOnline(long amount)
        {
            Values.Add(new BotCommandValue
            {
                Type = ECommandType.ChangeMoneyToAllOnline,
                Value = amount.ToString(),
                CheckTargetOnline = false
            });
            return this;
        }

        public override string ToString()
        {
            return string.Join(';', Values?.Select(x => x.Type.ToString()) ?? []);
        }
    }
}
