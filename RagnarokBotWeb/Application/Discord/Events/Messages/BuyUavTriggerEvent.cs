using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages
{
    public class BuyUavTriggerEvent : IMessageEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public BuyUavTriggerEvent(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task HandleAsync(SocketMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(SocketMessageComponent component)
        {
            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var botService = scope.ServiceProvider.GetRequiredService<IBotService>();
            var scumRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
            var server = await scumRepository.FindByGuildId(component.GuildId!.Value);

            if (DiscordEventService.UserUavSelections.TryGetValue((component.User.Id, component.GuildId.Value), out var selectedZone))
            {
                if (selectedZone == "0") await component.DeferAsync(ephemeral: true);

                if (!botService.IsBotOnline(server!.Id))
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Order failed")
                        .WithDescription("There is no active bots at the moment. Please try again later.")
                        .WithColor(Color.Red)
                        .Build();
                    await component.RespondAsync(embed: embed, ephemeral: true);
                    return;
                }
                try
                {
                    var order = await orderService.PlaceUavOrderFromDiscord(server, component.User.Id, selectedZone);
                    var embed = new EmbedBuilder()
                      .WithTitle("UAV Scan")
                      .WithDescription($"Your order with number #{order!.Id} was registered. Executing UAV Scan on sector {selectedZone}.\nYour current coin balance: {order.BalancePreview}\n{order.ResolveCooldownText(order.GetItem())}")
                      .WithColor(Color.Green)
                      .Build();

                    await component.RespondAsync(embed: embed, ephemeral: true);
                    await orderService.ProcessOrder(order);
                    return;
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
                catch (DomainException ex)
                {
                    var embed = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription(ex.Message)
                    .WithColor(Color.Red)
                    .Build();
                    await component.RespondAsync(embed: embed, ephemeral: true);
                }
                catch (Exception ex)
                {
                    var embed = new EmbedBuilder()
                    .WithDescription(ex.Message)
                    .WithTitle("Something went wrong. Please try again later.")
                    .WithColor(Color.Red)
                    .Build();
                    await component.RespondAsync(embed: embed, ephemeral: true);
                }
            }
            else
            {
                await component.RespondAsync("No zone selected yet. Please choose from the dropdown first.", ephemeral: true);
            }
        }

        public Task HandleAsync(SocketModal message)
        {
            throw new NotImplementedException();
        }
    }
}
