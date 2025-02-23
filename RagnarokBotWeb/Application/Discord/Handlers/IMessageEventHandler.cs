using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public interface IMessageEventHandler
{
    Task HandleAsync(SocketMessage message);
}