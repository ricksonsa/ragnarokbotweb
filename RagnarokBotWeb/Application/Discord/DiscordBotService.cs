using Discord;
using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordBotService(ILogger<DiscordBotService> logger, DiscordSocketClient client) : BackgroundService
{
    // private readonly string? _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
    private readonly string? _token = "MTM0MDE3NjI0MTA5NzE4MzM0Mw.Gj_E-C.cP30r5RLRLnXwE6bAzWBfCrN2dVXn52J21MHoY";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.Log += LogAsync;

        if (string.IsNullOrEmpty(_token))
        {
            logger.LogError("Discord token is not set!");
            return;
        }

        await client.LoginAsync(TokenType.Bot, _token);
        await client.StartAsync();

        logger.LogInformation("Discord bot started.");

        client.Ready += () =>
        {
            logger.LogInformation("Discord bot ready.");
            logger.LogInformation("Discord bot has the following Guilds:");
            foreach (var guild in client.Guilds)
                logger.LogInformation(
                    $"Guild Id: {guild.Id}, Guild Name: {guild.Name}, Guild Created: {guild.CreatedAt}");
            return Task.CompletedTask;
        };

        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.LogoutAsync();
        await client.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    private Task LogAsync(LogMessage log)
    {
        logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }
}