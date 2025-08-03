
namespace RagnarokBotWeb.Domain.Enums
{
    public static class ChannelTemplateValues
    {
        public static readonly ChannelTemplateValue None = new("none");
        public static readonly ChannelTemplateValue Chat = new("chat");
        public static readonly ChannelTemplateValue GameChat = new("game-chat");
        public static readonly ChannelTemplateValue NoAdminAbusePublic = new("no-admin-abuse-public");
        public static readonly ChannelTemplateValue KillFeed = new("kill-feed");
        public static readonly ChannelTemplateValue BunkerActivation = new("bunker-states");
        public static readonly ChannelTemplateValue WelcomePack = new("register");
        public static readonly ChannelTemplateValue Taxi = new("taxi");
        public static readonly ChannelTemplateValue KillRank = new("kill-rank");
        public static readonly ChannelTemplateValue SniperRank = new("sniper-rank");
        public static readonly ChannelTemplateValue TopKillerDay = new("top-killer-rank");
        public static readonly ChannelTemplateValue LockPickRank = new("lockpick-rank");
        public static readonly ChannelTemplateValue TopLockpickDay = new("top-lockpick-rank");
        public static readonly ChannelTemplateValue NoAdminAbusePrivate = new("no-admin-abuse-private");
        public static readonly ChannelTemplateValue AdminAlert = new("admin-alert");
        public static readonly ChannelTemplateValue Login = new("login");
        public static readonly ChannelTemplateValue BuriedChest = new("buried-chest");
        public static readonly ChannelTemplateValue MineKill = new("mine-kill");
        public static readonly ChannelTemplateValue LockpickAlert = new("lockpick-alert");
        public static readonly ChannelTemplateValue AdminKill = new("admin-kill");
        public static readonly ChannelTemplateValue LockpickAdmin = new("lockpick-admin");
    }

    public class ChannelTemplateValue
    {
        private readonly string _value;
        public ChannelTemplateValue(string value) => _value = value;

        public static ChannelTemplateValue FromValue(string channelType)
        {
            return new ChannelTemplateValue(channelType);
        }

        public override string ToString()
        {
            return _value;
        }
    }
}
