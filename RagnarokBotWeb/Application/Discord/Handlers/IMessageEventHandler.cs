using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public interface IMessageEventHandler
{
    Task HandleAsync(SocketMessage message);
    Task HandleAsync(SocketMessageComponent message);
    Task HandleAsync(SocketModal message);
}