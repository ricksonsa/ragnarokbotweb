namespace RagnarokBotWeb.Domain.Enums
{
    [Flags]
    public enum AccessLevel
    {
        Default = 0,
        Mod = 1 << 0,          // 1
        Administrator = Mod | (1 << 1) // 1 | 2 = 3
    }
}
