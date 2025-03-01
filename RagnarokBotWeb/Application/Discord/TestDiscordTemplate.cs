using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class TestDiscordTemplate(ILogger<TestDiscordTemplate> logger, IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (DiscordBotService.Instance is null || !DiscordBotService.Instance.IsReady)
        {
            logger.LogInformation("DiscordBotService is not ready yet.");
            await Task.Delay(1000, stoppingToken);
        }

        using var scope = serviceProvider.CreateScope();
        var template = scope.ServiceProvider.GetRequiredService<StartupDiscordTemplate>();
        var guildService = scope.ServiceProvider.GetRequiredService<IGuildService>();
        var guild = await guildService.FindByGuildIdAsync(1L);
        await template.Run(guild);
        guild.RunTemplate = true;
        await guildService.Update(guild);
    }
}