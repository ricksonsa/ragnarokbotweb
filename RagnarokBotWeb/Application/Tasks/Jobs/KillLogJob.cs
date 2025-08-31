using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using SixLabors.ImageSharp;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class KillLogJob(
    ILogger<KillLogJob> logger,
    IScumServerRepository scumServerRepository,
    IUnitOfWork unitOfWork,
    IDiscordService discordService,
    IFileService fileService,
    IFtpService ftpService,
    ICacheService cacheService,
    IBotService botService
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

        try
        {
            var server = await GetServerAsync(context);
            var processor = new ScumFileProcessor(server, unitOfWork);

            string lastLine = string.Empty;

            await foreach (var line in processor.UnreadFileLinesAsync(GetFileTypeFromContext(context), ftpService, context.CancellationToken))
            {
                if (!line.Contains('{'))
                {
                    lastLine = line;
                    continue;
                }

                Kill? kill = null;
                try
                {
                    kill = new KillLogParser(server).Parse(lastLine, line);
                    if (kill is null) throw new Exception("Could not parse kill");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error parsing kill");
                    continue;
                }

                var squads = cacheService.GetSquads(server.Id);
                kill.IsSameSquad = IsSameSquad(kill, squads);

                if (IsCompliant())
                {
                    _ = Task.Run(async () => await HandleKillFeed(logger, discordService, fileService, server, kill));
                    _ = Task.Run(async () => await HandleCoinManager(logger, unitOfWork, server, kill));
                    _ = Task.Run(async () => await HandleAnnounceText(botService, server, kill));
                }

                try
                {
                    var dbContext = unitOfWork.CreateDbContext();
                    dbContext.ScumServers.Attach(server);
                    await dbContext.Kills.AddAsync(kill);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "KillLogJob Persistance Exception");
                }

            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Job} Exception", context.JobDetail.Key.Name);
            throw;
        }
    }

    private static async Task HandleCoinManager(ILogger<KillLogJob> logger, IUnitOfWork unitOfWork, ScumServer server, Kill kill)
    {
        try
        {
            unitOfWork.CreateDbContext();
            var coinHandler = new PlayerCoinManager(unitOfWork);

            if (server.CoinKillAwardAmount > 0 && !kill.IsSameSquad)
                await coinHandler.AddCoinsBySteamIdAsync(kill.KillerSteamId64!, server.Id, server.CoinKillAwardAmount);

            if (server.CoinDeathPenaltyAmount > 0 && !kill.IsSameSquad)
                await coinHandler.RemoveCoinsBySteamIdAsync(kill.TargetSteamId64!, server.Id, server.CoinDeathPenaltyAmount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PlayerCoinManager Exception");
        }
    }

    private static async Task HandleKillFeed(ILogger<KillLogJob> logger, IDiscordService discordService, IFileService fileService, ScumServer server, Kill kill)
    {
        try
        {
            if (server.UseKillFeed)
            {
                bool postKillFeed = true;

                if (!server.ShowSameSquadKill && kill.IsSameSquad) postKillFeed = false;

                if (Kill.IsMine(kill.Weapon) && !server.ShowMineKill)
                    postKillFeed = false;

                if (postKillFeed)
                {
                    if (server.ShowKillOnMap) await HandleShowMap(logger, fileService, kill);
                    _ = discordService.SendKillFeedEmbed(server, kill);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HandleKillFeed Exception");
        }
    }

    private static bool IsSameSquad(Kill kill, List<Shared.Models.ScumSquad> squads)
    {
        bool killerInSquad = false;
        bool targetInSameSquad = false;

        foreach (var squad in squads)
        {
            var memberIds = squad.Members.Select(m => m.SteamId).ToHashSet();

            if (memberIds.Contains(kill.KillerSteamId64!))
                killerInSquad = true;

            if (memberIds.Contains(kill.TargetSteamId64!))
                targetInSameSquad = true;

            // Both are in the same squad, skip
            if (killerInSquad && targetInSameSquad) return true;
        }

        return false;
    }

    private static async Task HandleAnnounceText(IBotService botService, ScumServer server, Kill kill)
    {
        if (!string.IsNullOrEmpty(server.KillAnnounceText))
        {
            var msg = server.KillAnnounceText!
                .Replace("{killer_name}", kill.KillerName)
                .Replace("{victim_name}", kill.TargetName)
                .Replace("{distance}", kill.Distance.ToString())
                .Replace("{weapon}", kill.DisplayWeapon)
                .Replace("{sector}", kill.Sector);

            await botService.SendCommand(server.Id, new Shared.Models.BotCommand().Say(msg));
        }
    }

    private static async Task HandleShowMap(ILogger<KillLogJob> logger, IFileService fileService, Kill kill)
    {
        try
        {
            await ExtractMap(fileService, kill);
        }
        catch (Exception ex)
        {
            logger.LogError("Error generating coordinates image -> {Ex}", ex.Message);
        }
    }

    private static async Task ExtractMap(IFileService fileService, Kill kill)
    {
        var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island_4k.jpg"));

        var center = kill.KillerX == 0 ? ScumCoordinate.MidPoint((kill.VictimX, kill.VictimY), (kill.VictimX, kill.VictimY))
            : ScumCoordinate.MidPoint((kill.KillerX, kill.KillerY), (kill.VictimX, kill.VictimY));

        var result = await extractor.ExtractMapWithPoints(
            center,
            [
                new ScumCoordinate(kill.KillerX, kill.KillerY, Color.Red).WithLabel(kill.KillerName!),
                new ScumCoordinate(kill.VictimX, kill.VictimY, Color.Black).WithLabel(kill.TargetName!)
            ]);
        kill.ImageUrl = await fileService.SaveImageStreamAsync(result, "image/jpg", storagePath: "cdn-storage/eliminations", cdnUrlPrefix: "images/eliminations");
    }
}