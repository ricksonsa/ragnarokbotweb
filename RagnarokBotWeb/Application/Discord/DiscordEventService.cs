using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordEventService(
    ILogger<DiscordEventService> logger,
    DiscordSocketClient client,
    IMessageEventHandlerFactory messageHandlerFactory,
    IInteractionEventHandlerFactory interactionHandlerFactory)
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

    private Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return Task.CompletedTask;

        var handler = messageHandlerFactory.GetHandler(message);

        if (handler is not null) return handler.HandleAsync(message);

        logger.LogTrace("Handler for message with content '{}' was not found", message.Content);
        return Task.CompletedTask;
    }

    private Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        var handler = interactionHandlerFactory.GetHandler(interaction);
        if (handler is not null) handler.HandleAsync(interaction);
        return Task.CompletedTask;
    }
}