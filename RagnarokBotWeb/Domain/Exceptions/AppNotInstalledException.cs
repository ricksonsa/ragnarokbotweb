namespace RagnarokBotWeb.Domain.Exceptions
{
    public class AppNotInstalledException : Exception
    {
        public AppNotInstalledException(ulong guildId) : base($"Bot not installed to Guild: {guildId}") { }
    }
}
