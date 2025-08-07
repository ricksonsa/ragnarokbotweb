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
        { "buy_package", (serviceProvider) => new BuyPackageEvent(serviceProvider) },
        { "buy_warzone", (serviceProvider) => new BuyWarzoneEvent(serviceProvider) },
        { "uav_scan_trigger", (serviceProvider) => new BuyUavTriggerEvent(serviceProvider) },
        { "uav_zone_select", (serviceProvider) => new UavSelectEvent(serviceProvider) }
    };

    public IMessageEventHandler? GetHandler(SocketMessage message)
    {
        return _handlers.TryGetValue(message.Content, out var factory) ? factory(_serviceProvider) : null;
    }

    public IMessageEventHandler? GetHandler(SocketMessageComponent component)
    {
        var message = component.Data.CustomId.Contains(':') ? component.Data.CustomId.Split(":")[0] : component.Data.CustomId;
        return _handlers.TryGetValue(message, out var factory) ? factory(_serviceProvider) : null;
    }
}