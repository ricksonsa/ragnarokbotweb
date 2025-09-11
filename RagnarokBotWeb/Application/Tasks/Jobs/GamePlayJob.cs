using Discord;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class GamePlayJob(
    ILogger<GamePlayJob> logger,
    IScumServerRepository scumServerRepository,
    IServiceProvider services,
    IFtpService ftpService,
    IFileService fileService,
    IDiscordService discordService,
    ICacheService cache,
    IBotService botService
) : AbstractJob(scumServerRepository), IFtpJob
{
    public async Task Execute(long serverId, EFileType fileTye)
    {
        logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
        try
        {
            var server = await GetServerAsync(serverId);

            using var scope = services.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var processor = new ScumFileProcessor(server, uow);

            await foreach (var line in processor.UnreadFileLinesAsync(fileTye, ftpService))
            {
                try
                {
                    if (IsCompliant())
                        await HandleArmedTrap(botService, services, cache, server, line, logger);

                    await HandleLockpick(services, discordService, fileService, server, line, logger);
                    await HandleBunkerState(services, server, line, logger);
                }
                catch (Exception ex)
                {
                    // Catch per-line exceptions so the loop continues
                    logger.LogError(ex, "Error processing line: {Line}", line);
                }
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
        }
    }

    private static async Task HandleLockpick(IServiceProvider services, IDiscordService discordService, IFileService fileService, ScumServer server, string line, ILogger<GamePlayJob> logger)
    {
        if (!line.Contains("[LogMinigame] [LockpickingMinigame_C]") &&
            !line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
            return;

        using var scope = services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var lockpick = new LockpickLogParser(server).Parse(line);
        if (lockpick is null || IsDiscardLockpickType(lockpick)) return;

        try
        {
            uow.ScumServers.Attach(server);
            await uow.Lockpicks.AddAsync(new Lockpick
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
            await uow.SaveAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HandleLockpick Persistence Exception");
        }

        if (server.SendVipLockpickAlert && server.Tenant.IsCompliant())
        {
            try
            {
                var player = await uow.Players
                    .Include(p => p.Vips)
                    .FirstOrDefaultAsync(p => p.SteamId64 == lockpick.OwnerSteamId);

                if (player is not null && player.IsVip() && player.DiscordId.HasValue)
                {
                    var centerCoord = new ScumCoordinate(lockpick.X, lockpick.Y);
                    var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island_4k.jpg"));
                    var result = await extractor.ExtractMapWithPoints(
                        centerCoord,
                        [new ScumCoordinate(lockpick.X, lockpick.Y).WithLabel(lockpick.TargetObject)]
                    );

                    var imageUrl = await fileService.SaveImageStreamAsync(
                        result,
                        "image/jpg",
                        "cdn-storage/lockpicks",
                        "images/lockpicks"
                    );

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

    private static bool IsDiscardLockpickType(LockpickLog lockpick) =>
        lockpick.TargetObject.Contains("BPLockpick_Medical_Container") ||
        lockpick.TargetObject.Contains("BPLockpick_NPP_DepletedUraniumStorage") ||
        lockpick.TargetObject.Contains("BPLockpick_Hazmat_Suit_Locker");

    private static async Task HandleBunkerState(IServiceProvider services, ScumServer server, string line, ILogger<GamePlayJob> logger)
    {
        try
        {
            if (!line.Contains("[LogBunkerLock]") || !line.Contains(" is "))
                return;

            var (sector, state, time) = new BunkerLogParser().Parse(line);

            using var scope = services.CreateScope();
            var bunkerRepository = scope.ServiceProvider.GetRequiredService<IBunkerRepository>();

            var bunker = await bunkerRepository.FindOneWithServerAsync(b => b.Sector == sector && b.ScumServer.Id == server.Id);
            bunker ??= new(sector);
            bunker.Locked = state;
            bunker.Available = DateTime.UtcNow.Add(time);
            bunker.ScumServer ??= server;

            await bunkerRepository.CreateOrUpdateAsync(bunker);
            await bunkerRepository.SaveAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HandleBunkerState Exception");
        }
    }

    private static async Task HandleArmedTrap(IBotService botService, IServiceProvider services, ICacheService cache, ScumServer server, string line, ILogger<GamePlayJob> logger)
    {
        try
        {
            if (!line.Contains("[LogTrap] Armed"))
                return;

            var trapLog = TrapLogParser.Parse(line);
            if (trapLog is null || !IsMineTrap(trapLog))
                return;

            var trapCoordinate = new ScumCoordinate(trapLog.X, trapLog.Y, trapLog.Z);

            if (!server.AllowMinesOutsideFlag)
            {
                var squad = cache.GetSquads(server.Id).FirstOrDefault(s => s.Members.Any(m => m.SteamId == trapLog.SteamId));
                var squadLeader = squad?.Members.FirstOrDefault(m => m.MemberRank == 4);
                var flag = cache.GetFlags(server.Id).FirstOrDefault(f =>
                    (squadLeader != null && f.SteamId == squadLeader.SteamId) ||
                    f.SteamId == trapLog.SteamId);

                if (flag is not null)
                {
                    if (!new ScumCoordinate(flag.X, flag.Y, flag.Z).IsInsideCube(trapCoordinate, 7050f))
                    {
                        logger.LogInformation(
                            "HandleArmedTrap triggered for server {Server} player {Player} TrapLocation {Location}",
                            server.Id, trapLog.User, trapCoordinate);

                        var msg = $"{trapLog.User} armed a mine outside flag area!";
                        var command = new Shared.Models.BotCommand()
                            .Teleport(trapLog.SteamId, trapCoordinate.ToString(), checkTargetOnline: true);

                        if (server.CoinReductionPerInvalidMineKill > 0)
                        {
                            msg += " Coin penalty applied.";
                            using var scope = services.CreateScope();
                            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                            await new PlayerCoinManager(uow).RemoveCoinsBySteamIdAsync(trapLog.SteamId, server.Id, server.CoinReductionPerInvalidMineKill);
                        }

                        if (server.AnnounceMineOutsideFlag)
                            command.Say(msg);

                        await botService.SendCommand(server.Id, command);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HandleArmedTrap Exception");
        }
    }

    private static bool IsMineTrap(TrapLog trapLog) =>
        trapLog.TrapName.Contains("Anti-personnel mine") ||
        trapLog.TrapName.Contains("PROM-1 Mine") ||
        trapLog.TrapName.Contains("Improvised Mine") ||
        trapLog.TrapName.Contains("Small anti-personnel mine");
}
