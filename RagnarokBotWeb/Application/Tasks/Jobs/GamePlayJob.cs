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
    IFtpService ftpService,
    IFileService fileService,
    IDiscordService discordService,
    ICacheService cache,
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
            await foreach (var line in processor.UnreadFileLinesAsync(GetFileTypeFromContext(context), ftpService, context.CancellationToken))
            {
                if (IsCompliant()) await HandleArmedTrap(botService, unitOfWork, cache, server, line, logger);
                await HandleLockpick(unitOfWork, discordService, fileService, server, line, logger);
                await HandleBunkerState(bunkerService, server, line);
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError("{Job} Exception -> {Ex} {Stack}", context.JobDetail.Key.Name, ex.Message, ex.StackTrace);
        }
    }

    private static async Task HandleLockpick(IUnitOfWork uow, IDiscordService discordService, IFileService fileService, ScumServer server, string line, ILogger<GamePlayJob> logger)
    {
        if (line.Contains("[LogMinigame] [LockpickingMinigame_C]") ||
            line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
        {
            var lockpick = LockpickLogParser.Parse(line);
            if (lockpick is null || IsDiscardLockpickType(lockpick)) return;

            try
            {
                var dbContext = uow.CreateDbContext();
                dbContext.ScumServers.Attach(server);
                await dbContext.Lockpicks.AddAsync(new Lockpick
                {
                    LockType = lockpick.LockType,
                    TargetObject = lockpick.TargetObject,
                    Attempts = lockpick.FailedAttempts,
                    Name = lockpick.User,
                    ScumId = lockpick.ScumId,
                    SteamId64 = lockpick.SteamId,
                    AttemptDate = DateTime.UtcNow,
                    ScumServer = server,
                    Success = lockpick.Success

                });
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HandleLockpick Persistance Exception");
            }

            if (server.SendVipLockpickAlert && server.Tenant.IsCompliant())
            {
                try
                {
                    var player = await uow.Players.Include(player => player.Vips).FirstOrDefaultAsync(player => player.SteamId64 == lockpick.OwnerSteamId);
                    if (player is not null && player.IsVip() && player.DiscordId.HasValue)
                    {
                        var centerCoord = new ScumCoordinate(lockpick.X, lockpick.Y);
                        var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island_4k.jpg"));
                        var result = await extractor.ExtractMapWithPoints(centerCoord, [new ScumCoordinate(lockpick.X, lockpick.Y).WithLabel(lockpick.TargetObject)]);
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
                                new CreateEmbedField("Unlocked", lockpick.Success ? "Yes" : "No", true)
                            ],
                            Title = "THE SCUM BOT ALERT",
                            Text = "Someone is trying to pick one of your locks!!!"
                        };
                        await discordService.SendEmbedToUserDM(embed);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Send Vip Alert Exception");
                }
            }
        }
    }

    private static bool IsDiscardLockpickType(LockpickLog lockpick)
    {
        return lockpick.TargetObject.Contains("BPLockpick_Medical_Container")
            || lockpick.TargetObject.Contains("BPLockpick_NPP_DepletedUraniumStorage")
            || lockpick.TargetObject.Contains("BPLockpick_Hazmat_Suit_Locker");
    }

    private static async Task HandleBunkerState(IBunkerService bunkerService, ScumServer server, string line)
    {
        if (line.Contains("[LogBunkerLock]") && line.Contains(" is "))
        {
            var (sector, state, time) = new BunkerLogParser().Parse(line);
            await bunkerService.UpdateBunkerState(server, sector, state, time);
        }
    }

    private static async Task HandleArmedTrap(IBotService botService, IUnitOfWork unitOfWork, ICacheService cache, ScumServer server, string line, ILogger<GamePlayJob> logger)
    {
        if (line.Contains("[LogTrap] Armed"))
        {
            var trapLog = TrapLogParser.Parse(line);
            if (trapLog is not null && IsMineTrap(trapLog))
            {
                var trapCoordinate = new ScumCoordinate(trapLog.X, trapLog.Y, trapLog.Z);
                if (!server.AllowMinesOutsideFlag)
                {
                    var squad = cache.GetSquads(server.Id).FirstOrDefault(squad => squad.Members.Any(member => member.SteamId == trapLog.SteamId));
                    var squadLeader = squad?.Members.FirstOrDefault(member => member.MemberRank == 4);
                    var flag = cache.GetFlags(server.Id).FirstOrDefault(flag =>
                        (squadLeader != null && flag.SteamId == squadLeader.SteamId)
                        || flag.SteamId == trapLog.SteamId);

                    if (flag is not null)
                    {
                        if (!new ScumCoordinate(flag!.X, flag.Y, flag.Z).IsInsideCube(trapCoordinate, 7050f))
                        {
                            logger.LogInformation("HandleArmedTrap triggered for server {Server} player {Player} TrapLocation {Location}", server.Id, trapLog.User, trapCoordinate);
                            var msg = $"{trapLog.User} armed a mine outside flag area!";
                            var command = new Shared.Models.BotCommand()
                                .Teleport(trapLog.SteamId, trapCoordinate.ToString(), checkTargetOnline: true);

                            if (server.CoinReductionPerInvalidMineKill > 0)
                            {
                                msg += " Coin penalty applied.";
                                await new PlayerCoinManager(unitOfWork).RemoveCoinsBySteamIdAsync(trapLog.SteamId, server.Id, server.CoinReductionPerInvalidMineKill);
                            }

                            if (server.AnnounceMineOutsideFlag) command.Say(msg);

                            await botService.SendCommand(server.Id, command);
                        }
                    }
                }
            }
        }
    }

    private static bool IsMineTrap(TrapLog trapLog)
    {
        return trapLog.TrapName.Contains("Anti-personnel mine")
            || trapLog.TrapName.Contains("PROM-1 Mine")
            || trapLog.TrapName.Contains("Improvised Mine")
            || trapLog.TrapName.Contains("Small anti-personnel mine");
    }
}