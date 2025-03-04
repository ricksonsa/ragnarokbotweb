using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class TestDiscordTemplate(ILogger<TestDiscordTemplate> logger, IServiceProvider serviceProvider)
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

        // FIXME: temporally mark as false
        await MarkRunTemplateAsFalse(guild, guildService);

        await template.Run(guild);
        guild.RunTemplate = true;
        await guildService.Update(guild);
    }

    private static async Task MarkRunTemplateAsFalse(Guild guild, IGuildService guildService)
    {
        guild.RunTemplate = false;
        await guildService.Update(guild);
    }
}