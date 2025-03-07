using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordSocketClientUtils
{
    private static readonly ILogger<DiscordSocketClientUtils> Logger;

    static DiscordSocketClientUtils()
    {
        var factory = LoggerFactory.Create(builder => builder.AddConsole());
        Logger = factory.CreateLogger<DiscordSocketClientUtils>();
    }

    public static async Task AwaitDiscordSocketClientIsReady(CancellationToken stoppingToken)
    {
        while (DiscordBotService.Instance is null || !DiscordBotService.Instance.IsReady)
        {
            Logger.LogInformation("DiscordBotService is not ready yet.");
            await Task.Delay(1000, stoppingToken);
        }
    }
    
    public static ulong? GetGuildDiscordId(SocketMessage message)
    {
        if (message.Channel is SocketGuildChannel channel) return channel.Guild.Id;
        return null;
    }
}