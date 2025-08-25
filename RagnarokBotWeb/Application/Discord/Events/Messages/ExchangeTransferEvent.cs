using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages;

public class ExchangeTransferEvent : IMessageEventHandler
{
    private readonly IServiceProvider _serviceProvider;

    public ExchangeTransferEvent(IServiceProvider serviceProvider)
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
        embedBuilder.WithTitle("Transfer");

        if (server?.Exchange is null)
        {
            embedBuilder.WithColor(Color.Red);
            embedBuilder.WithDescription("Invalid server");
            await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        if (player is null)
        {
            embedBuilder.WithColor(Color.Red);
            embedBuilder.WithDescription("You are not yet registered, please register using the Welcome Pack");
            await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        if (!server.Exchange.AllowTransfer)
        {
            embedBuilder.WithColor(Color.Red);
            embedBuilder.WithDescription("Coin transfer between players is disabled");
            await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
            return;
        }

        if (message.Data.CustomId == "transfer_modal")
        {
            var steamId = message.Data.Components
                .First(x => x.CustomId == "transfer_steam_id").Value;

            if (player.SteamId64 == steamId)
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("You cannot transfer coins to yourself");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            var targetPlayer = await uow.Players
              .Include(player => player.ScumServer)
              .Include(player => player.ScumServer.Exchange)
              .Include(player => player.ScumServer.Guild)
              .FirstOrDefaultAsync(
                  player => player.ScumServer != null
                  && player.ScumServer.Guild != null
                  && player.ScumServer.Guild.DiscordId == message.GuildId!.Value
                  && player.SteamId64 == steamId);

            if (targetPlayer is null)
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("The player you are trying to transfer to is not registered");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }

            var amount = message.Data.Components
                .First(x => x.CustomId == "transfer_amount").Value;

            if (long.TryParse(amount, out var value))
            {
                if (player.Coin < value)
                {
                    embedBuilder.WithColor(Color.Red);
                    embedBuilder.WithDescription("You don't have the amount of coins you are trying to transfer");
                    await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                    return;
                }

                await manager.RemoveCoinsBySteamIdAsync(player.SteamId64!, player.ScumServerId, value);
                await manager.AddCoinsBySteamIdAsync(targetPlayer.SteamId64!, targetPlayer.ScumServerId, value);

                embedBuilder.WithColor(Color.Green);
                embedBuilder.WithDescription($"You have successfully transfered {value} coins to {targetPlayer.Name}");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }
            else
            {
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription("Please input a valid amount for transfer");
                await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
                return;
            }
        }

        embedBuilder.WithColor(Color.Red);
        embedBuilder.WithDescription("Something went wrong, please try again later.");
        await message.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
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