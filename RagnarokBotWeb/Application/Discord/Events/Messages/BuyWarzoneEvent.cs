using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages;

public class BuyWarzoneEvent : IMessageEventHandler
{
    private readonly IServiceProvider _serviceProvider;

    public BuyWarzoneEvent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task HandleAsync(SocketMessage message) { throw new NotImplementedException("Invalid Handle Message Method"); }

    public async Task HandleAsync(SocketMessageComponent component)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var botService = scope.ServiceProvider.GetRequiredService<IBotService>();

        if (!await botService.IsBotOnline(component.GuildId!.Value))
        {
            var embed = new EmbedBuilder()
                 .WithTitle("Order failed")
                 .WithDescription("There is no active bots at the moment. Please try again later.")
                 .WithColor(Color.Red)
                 .Build();

            await component.RespondAsync(embed: embed, ephemeral: true);
        }

        try
        {
            var order = await orderService.PlaceWarzoneOrderFromDiscord(component.GuildId.Value, component.User.Id, long.Parse(component.Data.CustomId.Split(":")[1]));

            var embed = new EmbedBuilder()
              .WithTitle(order!.Warzone!.Name)
              .WithDescription(
                $"Your order with number #{order!.Id} was registered. You will be teleported to the event soon.{order.ResolveWarzoneCooldownText()}")
              .WithColor(Color.Green)
              .Build();

            await component.RespondAsync(embed: embed, ephemeral: true);
        }
        catch (NotFoundException)
        {
            var embed = new EmbedBuilder()
            .WithTitle("Error")
            .WithDescription("User not registered. Please register using #wecolmepack.")
            .WithColor(Color.Red)
            .Build();

            await component.RespondAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            var embed = new EmbedBuilder()
            .WithTitle("Error")
            .WithDescription(ex.Message)
            .WithColor(Color.Red)
            .Build();
            await component.RespondAsync(embed: embed, ephemeral: true);
        }
    }
}
