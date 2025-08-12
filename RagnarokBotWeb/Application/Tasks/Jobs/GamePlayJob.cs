using Discord;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class GamePlayJob(
    ILogger<GamePlayJob> logger,
    IScumServerRepository scumServerRepository,
    IBunkerService bunkerService,
    IUnitOfWork unitOfWork,
    IReaderPointerRepository readerPointerRepository,
    IFtpService ftpService,
    IFileService fileService,
    IDiscordService discordService,
    ICacheService cache
) : AbstractJob(scumServerRepository), IJob
{

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
        try
        {
            var server = await GetServerAsync(context);
            var processor = new ScumFileProcessor(server);
            await foreach (var line in processor.UnreadFileLinesAsync(GetFileTypeFromContext(context), readerPointerRepository, ftpService, context.CancellationToken))
            {
                if (IsCompliant()) await HandleArmedTrap(unitOfWork, cache, server, line);
                await HandleLockpick(unitOfWork, discordService, fileService, server, line);
                await HandleBunkerState(bunkerService, server, line);
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, this);
        }
    }

    private static async Task HandleLockpick(IUnitOfWork uow, IDiscordService discordService, IFileService fileService, ScumServer server, string line)
    {
        if (line.Contains("[LogMinigame] [LockpickingMinigame_C]") ||
            line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
        {
            var lockpick = LockpickLogParser.Parse(line);
            if (lockpick is null) return;
            uow.ScumServers.Attach(server);
            await uow.Lockpicks.AddAsync(new Lockpick
            {
                LockType = lockpick.LockType,
                Attempts = lockpick.FailedAttempts,
                Name = lockpick.User,
                ScumId = lockpick.ScumId,
                SteamId64 = lockpick.SteamId,
                AttemptDate = DateTime.UtcNow,
                ScumServer = server,
                Success = lockpick.Success

            });
            await uow.SaveAsync();

            if (server.SendVipLockpickAlert && server.Tenant.IsCompliant())
            {
                var player = await uow.Players.Include(player => player.Vips).FirstOrDefaultAsync(player => player.SteamId64 == lockpick.OwnerSteamId);
                if (player is not null && player.IsVip() && player.DiscordId.HasValue)
                {
                    var centerCoord = new ScumCoordinate(lockpick.X, lockpick.Y);
                    var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island_4k.jpg"));
                    var result = await extractor.ExtractMapWithPoints(
                        centerCoord,
                        [new ScumCoordinate(lockpick.X, lockpick.Y).WithLabel(lockpick.TargetObject)]);

                    var imageUrl = await fileService.SaveImageStreamAsync(result, "image/jpg", storagePath: "cdn-storage/lockpicks", cdnUrlPrefix: "images/lockpicks");
                    var embed = new CreateEmbed()
                    {
                        Color = Color.Red,
                        ImageUrl = imageUrl,
                        DiscordId = player.DiscordId.Value,
                        GuildId = server.Guild!.DiscordId,
                        Fields = [
                            new CreateEmbedField("Sector", centerCoord.GetSectorReference(), true),
                                new CreateEmbedField("Lock", lockpick.DisplayLockType, true),
                                new CreateEmbedField("Unlocked", lockpick.Success ? "Yes" : "No", true)],
                        Title = "THE SCUM BOT ALERT",
                        Text = "Warning!!! Someone is trying to pick one of your locks!!!"
                    };
                    await discordService.SendEmbedToUserDM(embed);
                }
            }
        }
    }

    private static async Task HandleBunkerState(IBunkerService bunkerService, ScumServer server, string line)
    {
        if (line.Contains("[LogBunkerLock]") && line.Contains(" is "))
        {
            var (sector, state, time) = new BunkerLogParser().Parse(line);
            await bunkerService.UpdateBunkerState(server, sector, state, time);
        }
    }

    private static async Task HandleArmedTrap(IUnitOfWork unitOfWork, ICacheService cache, ScumServer server, string line)
    {
        if (line.Contains("[LogTrap] Armed"))
        {
            var trapLog = TrapLogParser.Parse(line);
            if (trapLog is not null)
            {
                if (!server.AllowMinesOutsideFlag)
                {
                    var squad = cache.GetSquads(server.Id).FirstOrDefault(squad => squad.Members.Any(member => member.SteamId == trapLog.SteamId));
                    if (squad is null) return;

                    var squadeLeader = squad.Members.FirstOrDefault(member => member.MemberRank == 4);
                    if (squadeLeader is null) return;

                    var flag = cache.GetFlags(server.Id).FirstOrDefault(flag => flag.SteamId == squadeLeader.SteamId);
                    if (flag is null) return;

                    var distance = new ScumCoordinate(flag.X, flag.Y, flag.Z).DistanceTo(new ScumCoordinate(trapLog.X, trapLog.Y, trapLog.Z));
                    if (distance > 50)
                    {
                        var msg = $"{trapLog.User} armed a mine outside flag area!";
                        var command = new BotCommand()
                            .Teleport(trapLog.SteamId, $"{trapLog.X} {trapLog.Y} {trapLog.Z}", checkTargetOnline: true);

                        cache.GetCommandQueue(server.Id).Enqueue(command);

                        if (server.CoinReductionPerInvalidMineKill > 0)
                        {
                            msg += " Coin penalty applied.";
                            await new PlayerCoinManager(unitOfWork).RemoveCoinsBySteamIdAsync(trapLog.SteamId, server.Id, server.CoinReductionPerInvalidMineKill);
                        }

                        if (server.AnnounceMineOutsideFlag) command.Say(msg);

                    }
                }
            }
        }
    }
}