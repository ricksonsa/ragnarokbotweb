namespace RagnarokBotWeb.Domain.Enums
{
    public enum EChannelType
    {
        None = 0,
        
        // Public
        KillFeed = 1,
        TopKillerDay = 2,
        TopKillerGeneral = 3,
        NoAdminAbusePublic = 4,
        DailyPack = 5,
        WelcomePack = 6,
        
        // Admin
        NoAdminAbusePrivate = 100,
        Chat = 101,
        PublicAdminLog = 102,
        PublicChat = 103,
        Login = 104,
        BuriedChest = 105,
        MineKill = 106,
        AdminKill = 107,
    }
}
