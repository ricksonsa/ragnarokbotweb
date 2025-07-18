using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Events.Messages;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public class MessageEventHandlerFactory : IMessageEventHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    public MessageEventHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private readonly Dictionary<string, Func<IServiceProvider, IMessageEventHandler>> _handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "!dailypack", (serviceProvider) => new DailyPackEvent() },
        { "buy_package", (serviceProvider) => new BuyPackageEvent(serviceProvider) },
        { "buy_warzone", (serviceProvider) => new BuyWarzoneEvent(serviceProvider) }
    };

    public IMessageEventHandler? GetHandler(SocketMessage message)
    {
        return _handlers.TryGetValue(message.Content, out var factory) ? factory(_serviceProvider) : null;
    }

    public IMessageEventHandler? GetHandler(SocketMessageComponent component)
    {
        return _handlers.TryGetValue(component.Data.CustomId.Split(":")[0], out var factory) ? factory(_serviceProvider) : null;
    }
}