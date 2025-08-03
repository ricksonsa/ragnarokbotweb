using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Entities;
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

        var server = await GetServerAsync(context);

        try
        {
            var processor = new ScumFileProcessor(server);
            List<string> lines = [];

            await foreach (var line in processor.UnreadFileLinesAsync(GetFileTypeFromContext(context), readerPointerRepository, ftpService))
            {
                lines.Add(line);
            }
            using var enumerator = lines.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var first = enumerator.Current;
                if (!enumerator.MoveNext()) break;
                var second = enumerator.Current;

                Kill? kill = null;
                try
                {
                    kill = new KillLogParser(server).Parse(first, second);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error parsing kill -> {Ex}", ex.Message);
                    continue;
                }

                if (kill is null) continue;

                if (!server.ShowSameSquadKill)
                {
                    var squads = cacheService.GetSquads(server.Id);

                    bool killerInSquad = false;
                    bool targetInSameSquad = false;

                    foreach (var squad in squads)
                    {
                        var memberIds = squad.Members.Select(m => m.SteamId).ToHashSet();

                        if (memberIds.Contains(kill.KillerSteamId64!))
                            killerInSquad = true;

                        if (memberIds.Contains(kill.TargetSteamId64!))
                            targetInSameSquad = true;

                        if (killerInSquad && targetInSameSquad)
                        {
                            // Both are in the same squad, skip
                            continue;
                        }
                    }
                }

                if (server.UseKillFeed)
                {
                    if (server.ShowKillOnMap)
                    {
                        await HandleShowMap(logger, fileService, kill);
                    }

                    await discordService.SendKillFeedEmbed(server, kill);
                }

                if (!string.IsNullOrEmpty(server.KillAnnounceText))
                {
                    var msg = server.KillAnnounceText
                        .Replace("{killer_name}", kill.KillerName)
                        .Replace("{victim_name}", kill.TargetName)
                        .Replace("{distance}", kill.Distance.ToString())
                        .Replace("{weapon}", kill.DisplayWeapon)
                        .Replace("{sector}", kill.Sector);

                    cacheService.GetCommandQueue(server.Id).Enqueue(new BotCommand().Say(msg));
                }

                logger.LogDebug("Adding new kill entry: {Killer} -> {Target}", kill.KillerName, kill.TargetName);
                unitOfWork.ScumServers.Attach(server);
                await unitOfWork.Kills.AddAsync(kill);
                await unitOfWork.SaveAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError("{Job} Exception -> {Ex}", context.JobDetail.Key.Name, ex.Message);
        }
    }

    private static async Task HandleShowMap(ILogger<KillLogJob> logger, IFileService fileService, Kill? kill)
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
        var midPoint = ScumMapExtractor.GetMidpoint((kill.KillerX, kill.KillerY), (kill.VictimX, kill.VictimY));
        var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island.jpg"));
        var result = await extractor.ExtractMapWithPoints(
            new ScumCoordinate(midPoint.x, midPoint.y),
            [
                new ScumCoordinate(kill.KillerX, kill.KillerY, Color.Red),
                new ScumCoordinate(kill.VictimX, kill.VictimY, Color.Black)
            ],
            128);
        kill.ImageUrl = await fileService.SaveImageStreamAsync(result, "image/jpg", storagePath: "cdn-storage/eliminations", cdnUrlPrefix: "images/eliminations");
    }
}