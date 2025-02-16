using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public class MessageEventHandlerFactory : IMessageEventHandlerFactory
{
    private readonly Dictionary<string, Func<IMessageEventHandler>> _handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "!DailyPack", () => new DailyPackEvent() }
    };

    public IMessageEventHandler? GetHandler(SocketMessage message)
    {
        return _handlers.TryGetValue(message.Content, out var factory) ? factory() : null;
    }
}