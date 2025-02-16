using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public interface IMessageEventHandler
{
    Task HandleAsync(SocketMessage message);
}