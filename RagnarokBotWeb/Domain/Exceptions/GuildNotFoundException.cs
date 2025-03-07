namespace RagnarokBotWeb.Domain.Exceptions;

public class GuildNotFoundException(ulong discordId) : Exception($"Guild with DiscordId: '{discordId}' not found.");