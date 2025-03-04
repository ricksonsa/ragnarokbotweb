using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Events.Message;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public class MessageEventHandlerFactory : IMessageEventHandlerFactory
{
    private readonly Dictionary<string, Func<IMessageEventHandler>> _handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "!dailypack", () => new DailyPackEvent() }
    };

    public IMessageEventHandler? GetHandler(SocketMessage message)
    {
        return _handlers.TryGetValue(message.Content, out var factory) ? factory() : null;
    }
}