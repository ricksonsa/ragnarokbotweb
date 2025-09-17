using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Resolvers;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Globalization;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class LoginJob(
    ILogger<LoginJob> logger,
    IScumServerRepository scumServerRepository,
    IPlayerService playerService,
    IDiscordService discordService,
    SteamAccountResolver steamAccountResolver,
    IpAddressResolver ipAddressResolver,
    IChannelService channelService,
    IUnitOfWork unitOfWork,
    IServiceProvider services,
    IFtpService ftpService
    ) : AbstractJob(scumServerRepository), IFtpJob
{

    public async Task Execute(long serverId, EFileType fileType)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
        try
        {
            var server = await GetServerAsync(serverId);

            var processor = new ScumFileProcessor(server, unitOfWork);
            await foreach (var line in processor.UnreadFileLinesAsync(fileType, ftpService))
            {
                try
                {
                    var (Date, IpAddress, SteamId, PlayerName, ScumId, IsLoggedIn, X, Y, Z) = new LoginLogParser().Parse(line);
                    if (string.IsNullOrWhiteSpace(SteamId)) continue;

                    if (IsLoggedIn)
                    {
                        var player = await playerService.PlayerConnected(server, SteamId, ScumId, PlayerName, X, Y, Z, IpAddress);
                        var channel = await channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.Login);
                        if (channel != null)
                            await SendLoginNotification(discordService, steamAccountResolver, ipAddressResolver, IpAddress, SteamId, PlayerName, X, Y, Z, player, channel);
                    }
                    else
                    {
                        playerService.PlayerDisconnected(server.Id, SteamId);
                        using var scope = services.CreateScope();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var player = await uow.Players.FirstOrDefaultAsync(p => p.SteamId64 == SteamId);
                        var channel = await channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.Login);
                        if (channel != null)
                            await SendLogoutNotification(discordService, IpAddress, SteamId, player!.SteamName!, PlayerName, X, Y, Z, channel);

                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{JobKey} Exception", $"{GetType().Name}({serverId})");
                }

            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{JobKey} Exception", $"{GetType().Name}({serverId})");
        }
    }

    private static async Task SendLoginNotification(IDiscordService discordService, SteamAccountResolver steamAccountResolver, IpAddressResolver ipAddressResolver, string IpAddress, string SteamId, string PlayerName, double? X, double? Y, double? Z, Player player, Domain.Entities.Channel? channel)
    {
        var embed = new CreateEmbed(channel!.DiscordId)
        {
            Title = "Player Logged In",
        };
        var steamInfo = await steamAccountResolver.Resolve(SteamId);

        embed.AddField(new CreateEmbedField("Player", PlayerName, inline: true));
        embed.AddField(new CreateEmbedField("SteamId", SteamId, inline: true));
        embed.AddField(new CreateEmbedField("Steam Name", player.SteamName ?? steamInfo?.PersonaName ?? "Unknown", inline: true));

        if (steamInfo != null)
        {
            embed.AddField(new CreateEmbedField("Vac Banned", steamInfo.VacBanned ? "Yes" : "No", inline: true));
            embed.AddField(new CreateEmbedField("Vac Bans", steamInfo.NumberOfVacBans.ToString(), inline: true));
            embed.AddField(new CreateEmbedField("Days Since Last Ban", steamInfo.DaysSinceLastBan.ToString(), inline: true));
        }

        if (player.DiscordId != null && !string.IsNullOrEmpty(player.DiscordName))
            embed.AddField(new CreateEmbedField("Discord", player.DiscordName, inline: true));

        if (X.HasValue && Y.HasValue && Z.HasValue)
        {
            var location = new ScumCoordinate(X.Value, Y.Value, Z.Value);
            embed.AddField(new CreateEmbedField(
                "Spawn Location",
                $"Sector {location.GetSectorReference()} " +
                $"X={X.Value.ToString("F3", CultureInfo.InvariantCulture)} " +
                $"Y={Y.Value.ToString("F3", CultureInfo.InvariantCulture)} " +
                $"Z={Z.Value.ToString("F3", CultureInfo.InvariantCulture)}"
            ));
        }

        if (!string.IsNullOrEmpty(IpAddress))
        {
            var result = await ipAddressResolver.Resolve(IpAddress);
            if (result is not null)
            {
                embed.AddField(new CreateEmbedField("IpAddress", IpAddress, inline: true));
                embed.AddField(new CreateEmbedField("Country", result.Country!, inline: true));
                embed.AddField(new CreateEmbedField("Region Name", result.RegionName!, inline: true));
                embed.AddField(new CreateEmbedField("City", result.City!, inline: true));
            }
        }

        await discordService.SendEmbedToChannel(embed);
    }

    private static async Task SendLogoutNotification(IDiscordService discordService, string IpAddress, string SteamId, string SteamName, string PlayerName, double? X, double? Y, double? Z, Channel? channel)
    {
        var embed = new CreateEmbed(channel!.DiscordId)
        {
            Title = "Player Logged Out",
        };

        embed.AddField(new CreateEmbedField("Player", PlayerName, inline: true));
        embed.AddField(new CreateEmbedField("SteamId", SteamId, inline: true));
        embed.AddField(new CreateEmbedField("Steam Name", SteamName, inline: true));
        embed.AddField(new CreateEmbedField("IpAddress", IpAddress, inline: true));

        if (X.HasValue && Y.HasValue && Z.HasValue)
        {
            var location = new ScumCoordinate(X.Value, Y.Value, Z.Value);
            embed.AddField(new CreateEmbedField(
                "Logout Location",
                $"Sector {location.GetSectorReference()} " +
                $"X={X.Value.ToString("F3", CultureInfo.InvariantCulture)} " +
                $"Y={Y.Value.ToString("F3", CultureInfo.InvariantCulture)} " +
                $"Z={Z.Value.ToString("F3", CultureInfo.InvariantCulture)}"
            ));
        }

        await discordService.SendEmbedToChannel(embed);
    }
}