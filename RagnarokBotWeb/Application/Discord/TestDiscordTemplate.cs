using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Application.Discord;

public class TestDiscordTemplate(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var template = scope.ServiceProvider.GetRequiredService<StartupDiscordTemplate>();
        var guild = new Guild
        {
            Id = 1,
            Enabled = true,
            RunTemplate = false,
            DiscordId = 1343235141136552099
        };
        await template.Run(guild);
    }
}