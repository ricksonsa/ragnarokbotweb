using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages;

public class BuyPackageEvent : IMessageEventHandler
{
    private readonly IServiceProvider _serviceProvider;

    public BuyPackageEvent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task HandleAsync(SocketMessage message) { throw new NotImplementedException("Invalid Handle Message Method"); }

    public async Task HandleAsync(SocketMessageComponent component)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        try
        {
            var order = await orderService.PlaceOrder(component.User.Id.ToString(), long.Parse(component.Data.CustomId.Split(":")[1]));
            await component.Channel.SendMessageAsync($"Your order number ${order!.Id} was registered. Please wait in an open area until your order is delivered.");
        }
        catch (NotFoundException)
        {
            await component.Channel.SendMessageAsync($"User not registered, please register using #wecolmepack");
        }
        catch (Exception ex)
        {
            await component.Channel.SendMessageAsync($"Your request have failed [{ex.Message}]");
        }
    }
}