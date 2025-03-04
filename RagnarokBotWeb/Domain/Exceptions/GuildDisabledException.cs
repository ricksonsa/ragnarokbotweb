namespace RagnarokBotWeb.Domain.Exceptions;

public class GuildDisabledException(string message) : Exception(message)
{
}