using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordEventService(
    ILogger<DiscordEventService> logger,
    DiscordSocketClient client,
    IMessageEventHandlerFactory messageHandlerFactory,
    IInteractionEventHandlerFactory interactionHandlerFactory,
    IServiceProvider serviceProvider
)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DiscordSocketClientUtils.AwaitDiscordSocketClientIsReady(stoppingToken);

        client.MessageReceived += MessageReceivedAsync;
        client.InteractionCreated += InteractionCreatedAsync;

        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        var discordId = DiscordSocketClientUtils.GetGuildDiscordId(message);

        try
        {
            await ValidateGuildIsActiveAsync(discordId ?? 0L);

            var handler = messageHandlerFactory.GetHandler(message);
            handler?.HandleAsync(message);

            logger.LogTrace("Handler for message with content '{}' was not found", message.Content);
        }
        catch (Exception e)
        {
            logger.LogWarning(e,
                "Error when try process MessageReceived Action. Discord guild id = '{}', SocketMessage = '{}', ChannelType = '{}'",
                discordId, message.GetType().FullName, message.Channel.GetType().FullName);
        }
    }

    private async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            await ValidateGuildIsActiveAsync(interaction.GuildId ?? 0L);

            var handler = interactionHandlerFactory.GetHandler(interaction);
            handler?.HandleAsync(interaction);
        }
        catch (Exception e)
        {
            logger.LogWarning(e,
                "Error when try process InteractionCreated Action. Discord guild id = '{}', SocketInteraction = '{}', ChannelType = '{}'",
                interaction.GuildId, interaction.GetType().FullName, interaction.Channel.GetType().FullName);
        }
    }

    private async Task ValidateGuildIsActiveAsync(ulong discordId)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IGuildService>().ValidateGuildIsActiveAsync(discordId);
    }
}