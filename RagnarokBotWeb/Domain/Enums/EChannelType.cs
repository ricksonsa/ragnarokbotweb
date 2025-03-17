namespace RagnarokBotWeb.Domain.Enums
{
    public enum EChannelType
    {
        None = 0,

        // Public
        Chat = 1,
        GameChat = 13,
        NoAdminAbusePublic = 2,
        KillFeed = 3,
        BunkerActivation = 12,

        // Shop
        DailyPack = 4,
        WelcomePack = 5,
        Taxi = 6,

        // Ranks
        KillRank = 7,
        SniperRank = 8,
        TopKillerDay = 9,
        LockPickRank = 10,
        TopLockpickDay = 11,


        // Admin
        NoAdminAbusePrivate = 100,
        AdminAlert = 102,
        Login = 103,
        BuriedChest = 104,
        MineKill = 105,
        LockpickAlert = 106,
        AdminKill = 107,
    }
}
