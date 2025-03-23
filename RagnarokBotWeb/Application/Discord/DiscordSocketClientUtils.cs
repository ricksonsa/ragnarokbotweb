using Discord;
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

    public static async Task AwaitDiscordSocketClientIsReady(CancellationToken stoppingToken = default)
    {
        while (DiscordBotService.Instance is null || !DiscordBotService.Instance.IsReady)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                Logger.LogWarning("DiscordBotService->AwaitDiscordSocketClientIsReady Cancellation Requested.");
                break;
            }

            Logger.LogInformation("DiscordBotService is not ready yet.");
            await Task.Delay(1000, stoppingToken);
        }
    }

    public static ulong? GetGuildDiscordId(SocketMessage message)
    {
        if (message.Channel is SocketGuildChannel channel) return channel.Guild.Id;
        return null;
    }

    public static Optional<IEnumerable<Overwrite>> BuildPermissionOverwrites(SocketGuild guild, bool adminOnly = false)
    {
        var allAdminRoles = guild.Roles.Where(x => x.Permissions.Administrator).ToList();

        var everyonePerms = new OverwritePermissions(
            viewChannel: adminOnly ? PermValue.Deny : PermValue.Allow,
            sendMessages: PermValue.Deny
        );

        var adminPerms = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );

        var overwrites = allAdminRoles.Select(x => new Overwrite(x.Id, PermissionTarget.Role, adminPerms)).ToList();
        overwrites.Add(new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, everyonePerms));
        return overwrites;
    }
}