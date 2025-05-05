using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public interface IMessageEventHandlerFactory
{
    IMessageEventHandler? GetHandler(SocketMessage message);
    IMessageEventHandler? GetHandler(SocketMessageComponent message);
}