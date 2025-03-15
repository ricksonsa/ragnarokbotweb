using Discord.WebSocket;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class TestDiscordTemplate(
    ILogger<TestDiscordTemplate> logger,
    IServiceProvider serviceProvider,
    DiscordSocketClient client
)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DiscordSocketClientUtils.AwaitDiscordSocketClientIsReady(stoppingToken);

        using var scope = serviceProvider.CreateScope();
        var template = scope.ServiceProvider.GetRequiredService<StartupDiscordTemplate>();
        var guildService = scope.ServiceProvider.GetRequiredService<IGuildService>();
        var guild = await guildService.FindByGuildIdAsync(1L);
        if (guild is null)
        {
            logger.LogWarning("No guild found.");
            return;
        }

        await ClearBefore(guild);

        await template.Run(guild);
        guild.RunTemplate = true;
        await guildService.Update(guild);
    }

    private async Task ClearBefore(Guild guild)
    {
        guild.RunTemplate = false;

        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IGuildService>().Update(guild);
        await scope.ServiceProvider.GetRequiredService<IChannelService>().DeleteAllAsync();

        var socketGuild = client.GetGuild(guild.DiscordId);
        foreach (var channel in socketGuild.Channels) await channel.DeleteAsync();
        foreach (var category in socketGuild.CategoryChannels) await category.DeleteAsync();
    }
}