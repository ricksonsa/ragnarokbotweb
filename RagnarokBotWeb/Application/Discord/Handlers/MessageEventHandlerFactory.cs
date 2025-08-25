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
        { "buy_taxi", (serviceProvider) => new BuyTaxiEvent(serviceProvider) },
        { "uav_scan_trigger", (serviceProvider) => new BuyUavTriggerEvent(serviceProvider) },
        { "uav_zone_select", (serviceProvider) => new UavSelectEvent(serviceProvider) },
        { "taxi_telport_select", (serviceProvider) => new TaxiTeleportSelectEvent(serviceProvider) },
        { "wallet_balance", (serviceProvider) => new WalletBalanceEvent(serviceProvider) },
        { "transfer_trigger", (serviceProvider) => new ExchangePlayerTransferEventTrigger(serviceProvider) },
        { "withdraw_trigger", (serviceProvider) => new ExchangeWithdrawEventTrigger(serviceProvider) },
        { "deposit_trigger", (serviceProvider) => new ExchangeDepositEventTrigger(serviceProvider) },
        { "transfer_modal", (serviceProvider) => new ExchangeTransferEvent(serviceProvider) },
        { "withdraw_modal", (serviceProvider) => new ExchangeWithdrawEvent(serviceProvider) },
        { "deposit_modal", (serviceProvider) => new ExchangeDepositEvent(serviceProvider) }
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

    public IMessageEventHandler? GetHandler(SocketModal component)
    {
        var message = component.Data.CustomId.Contains(':') ? component.Data.CustomId.Split(":")[0] : component.Data.CustomId;
        return _handlers.TryGetValue(message, out var factory) ? factory(_serviceProvider) : null;
    }
}