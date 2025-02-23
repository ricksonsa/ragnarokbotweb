using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Events;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public class MessageEventHandlerFactory : IMessageEventHandlerFactory
{
    private readonly Dictionary<string, Func<IMessageEventHandler>> _handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        //{ "!welcomepack", () => new DailyPackEvent() },
        { "!dailypack", () => new DailyPackEvent() }
    };

    public IMessageEventHandler? GetHandler(SocketMessage message)
    {
        return _handlers.TryGetValue(message.Content, out var factory) ? factory() : null;
    }
}