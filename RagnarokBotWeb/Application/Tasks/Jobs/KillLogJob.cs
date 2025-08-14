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
    IReaderPointerRepository readerPointerRepository,
    IDiscordService discordService,
    IFileService fileService,
    IFtpService ftpService,
    ICacheService cacheService
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

        try
        {
            var server = await GetServerAsync(context);
            var processor = new ScumFileProcessor(server);

            string lastLine = string.Empty;

            await foreach (var line in processor.UnreadFileLinesAsync(GetFileTypeFromContext(context), readerPointerRepository, ftpService, context.CancellationToken))
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
                    logger.LogError("Error parsing kill -> {Ex}", ex.Message);
                    continue;
                }

                var squads = cacheService.GetSquads(server.Id);
                kill.IsSameSquad = IsSameSquad(kill, squads);

                if (IsCompliant())
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
                            await discordService.SendKillFeedEmbed(server, kill);
                        }
                    }

                    var coinHandler = new PlayerCoinManager(unitOfWork);
                    if (server.CoinKillAwardAmount > 0 && !kill.IsSameSquad)
                        await coinHandler.AddCoinsBySteamIdAsync(kill.KillerSteamId64!, server.Id, server.CoinKillAwardAmount);

                    if (server.CoinDeathPenaltyAmount > 0 && !kill.IsSameSquad)
                        await coinHandler.RemoveCoinsBySteamIdAsync(kill.TargetSteamId64!, server.Id, server.CoinDeathPenaltyAmount);

                    HandleAnnounceText(cacheService, server, kill); // Announce kill in game
                }

                logger.LogDebug("Adding new kill entry: {Killer} -> {Target}", kill.KillerName, kill.TargetName);
                unitOfWork.ScumServers.Attach(server);
                await unitOfWork.Kills.AddAsync(kill);
                await unitOfWork.SaveAsync();
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError("{Job} Exception -> {Ex} {Stack}", context.JobDetail.Key.Name, ex.Message, ex.StackTrace);
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

    private static void HandleAnnounceText(ICacheService cacheService, ScumServer server, Kill kill)
    {
        if (!string.IsNullOrEmpty(server.KillAnnounceText))
        {
            var msg = server.KillAnnounceText!
                .Replace("{killer_name}", kill.KillerName)
                .Replace("{victim_name}", kill.TargetName)
                .Replace("{distance}", kill.Distance.ToString())
                .Replace("{weapon}", kill.DisplayWeapon)
                .Replace("{sector}", kill.Sector);

            cacheService.GetCommandQueue(server.Id).Enqueue(new BotCommand().Say(msg));
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