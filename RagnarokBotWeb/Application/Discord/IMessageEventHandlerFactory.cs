using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public interface IMessageEventHandlerFactory
{
    IMessageEventHandler? GetHandler(SocketMessage message);
}