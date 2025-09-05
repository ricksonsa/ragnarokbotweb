using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages;

public class ExchangeWithdrawEvent : IMessageEventHandler
{
    private readonly IServiceProvider _serviceProvider;

    public ExchangeWithdrawEvent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(SocketModal message)
    {

        var scope = _serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var manager = new PlayerCoinManager(uow);
        var scumRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
        var server = await scumRepository.FindByGuildId(message.GuildId!.Value);
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var botService = scope.ServiceProvider.GetRequiredService<IBotService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ExchangeWithdrawEvent>>();

        try
        {
            var player = await uow.Players
                .Include(player => player.ScumServer)
                .Include(player => player.ScumServer.Exchange)
                .Include(player => player.ScumServer.Guild)
                .FirstOrDefaultAsync(
                    player => player.ScumServer != null
                    && player.ScumServer.Guild != null
                    && player.ScumServer.Guild.DiscordId == message.GuildId!.Value
                    && player.DiscordId == message.User.Id);


            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("Withdraw");

            if (server?.Exchange is null)
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("Invalid server");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            if (player is null || !player.DiscordId.HasValue)
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("You are not yet registered, please register using the Welcome Pack");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            if (!botService.IsBotOnline(player.ScumServerId))
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("There is no active bots at the moment");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            if (!server.Exchange.AllowWithdraw)
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("Coin withdraw is disabled");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            if (message.Data.CustomId == "withdraw_modal")
            {
                var amount = message.Data.Components
                    .First(x => x.CustomId == "withdraw_amount").Value;

                if (long.TryParse(amount, out var value))
                {
                    if (player.Coin < value)
                    {
                        embedBuilder.WithColor(Color.Red);
                        embedBuilder.WithDescription($"You don't have the amount of {value} coins to withdraw");
                        await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                        return;
                    }

                    var money = new CoinConverterManager(server).ToInGameMoney(value);
                    await orderService.ExchangeWithdrawOrder(player.ScumServer.Id, player.DiscordId.Value, value);
                    embedBuilder.WithColor(Color.Green);
                    embedBuilder.WithDescription($"Your withdraw of {value} coins to in-game {server.Exchange.CurrencyType} will be processed soon.\n\nCurrent Balance: {player.Coin}\nNext Balance: {player.Coin - value}\nIn-game {server.Exchange.CurrencyType}: +{money}");
                    await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                    return;
                }
                else
                {
                    embedBuilder.WithColor(Color.Red);
                    embedBuilder.WithDescription("Please input a valid amount to withdraw");
                    await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                    return;
                }
            }

            embedBuilder.WithColor(Color.Red);
            embedBuilder.WithDescription("Something went wrong, please try again later.");
            await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ExchangeWithdrawEvent Exception");
            throw;
        }
    }

    public Task HandleAsync(SocketMessage message)
    {
        throw new NotImplementedException();
    }

    public Task HandleAsync(SocketMessageComponent message)
    {
        throw new NotImplementedException();
    }
}