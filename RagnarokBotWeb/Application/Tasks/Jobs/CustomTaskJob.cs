using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;
using static RagnarokBotWeb.Crosscutting.Utils.StringUtils;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{

    public class CustomTaskJob(
        ILogger<CustomTaskJob> logger,
        ICustomTaskRepository customTaskRepository,
        ICacheService cache,
        IUnitOfWork unitOfWork,
        IDiscordService discordService,
        IBotService botService,
        IScumServerRepository scumServerRepository) : AbstractJob(scumServerRepository), ICustomTaskJob
    {
        public async Task Execute(long serverId, long customTaskId)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(serverId, validateSubscription: true);
                var customTask = await customTaskRepository.FindByIdAsync(customTaskId);
                if (customTask is null) return;
                if (!customTask.Enabled) return;

                if (customTask.MinPlayerOnline.HasValue && customTask.MinPlayerOnline.Value > 0)
                {
                    var playerCount = cache.GetConnectedPlayers(server.Id).Count;
                    if (playerCount < customTask.MinPlayerOnline) return;
                }

                if (customTask.IsBlockPurchaseRaidTime)
                {
                    var raidTimes = cache.GetRaidTimes(server.Id);
                    if (raidTimes is not null && raidTimes.IsInRaidTime(server))
                        return;
                }

                if (!string.IsNullOrEmpty(customTask.StartMessage))
                {
                    await botService.SendCommand(server.Id, new BotCommand().Say(customTask.StartMessage));
                }

                if (string.IsNullOrEmpty(customTask.Commands)) return;

                List<BotCommand> commands = [];
                switch (customTask.TaskType)
                {
                    case Domain.Enums.ECustomTaskType.BatchCommandExecute:
                        foreach (var line in customTask.Commands.ToLines())
                            commands.Add(new BotCommand().SayOrCommand(line));
                        break;

                    case Domain.Enums.ECustomTaskType.ExecuteOneRandomly:
                        var lines = customTask.Commands.ToLines();
                        int index = new Random().Next(lines.Count());
                        commands.Add(new BotCommand().SayOrCommand(lines.ElementAt(index)));
                        break;

                    case Domain.Enums.ECustomTaskType.ServerSettings:
                        var fileChangeCommand = new FileChangeCommand()
                        {
                            ServerId = server.Id,
                            FileChangeMethod = Domain.Enums.EFileChangeMethod.UpdateLine,
                            FileChangeType = Domain.Enums.EFileChangeType.ServerSettings,
                            Key = customTask.Commands.Split('=')[0],
                            Value = customTask.Commands.Split('=')[1]
                        };
                        cache.EnqueueFileChangeCommand(server.Id, fileChangeCommand);
                        break;

                    case Domain.Enums.ECustomTaskType.Switch:
                        var switchType = customTask.Commands[..customTask.Commands.IndexOf(':')];
                        var switchValue = customTask.Commands.Split("=")[0].Substring(customTask.Commands.IndexOf(':') + 1);
                        var value = customTask.Commands.Substring(customTask.Commands.IndexOf('=') + 1);
                        var id = long.Parse(switchValue);
                        var enableDisable = value == "1";

                        if (switchType == "taxi")
                        {
                            var taxi = await unitOfWork.Taxis.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                            if (enableDisable == taxi!.Enabled) break;
                            await unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""Taxis"" SET ""Enabled"" = {enableDisable} WHERE ""Id"" = {id}");
                            if (taxi?.DiscordChannelId != null && taxi.DiscordMessageId.HasValue)
                            {
                                try { await discordService.RemoveMessage(ulong.Parse(taxi.DiscordChannelId), taxi.DiscordMessageId.Value); }
                                catch { }
                            }
                        }
                        else if (switchType == "package")
                        {

                            var package = await unitOfWork.AppDbContext.Packs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                            if (enableDisable == package!.Enabled) break;
                            await unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""Packs"" SET ""Enabled"" = {enableDisable} WHERE ""Id"" = {id}");
                            if (package?.DiscordChannelId != null && package.DiscordMessageId.HasValue)
                            {
                                try { await discordService.RemoveMessage(ulong.Parse(package.DiscordChannelId), package.DiscordMessageId.Value); }
                                catch { }
                            }
                        }
                        else if (switchType == "uav")
                        {
                            var uav = await unitOfWork.AppDbContext.Uavs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                            if (enableDisable == uav!.Enabled) break;
                            await unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""Uavs"" SET ""Enabled"" = {enableDisable} WHERE ""Id"" = {id}");
                            if (uav?.DiscordChannelId != null && uav.DiscordMessageId.HasValue)
                            {
                                try { await discordService.RemoveMessage(ulong.Parse(uav.DiscordChannelId), uav.DiscordMessageId.Value); }
                                catch { }
                            }
                        }
                        else if (switchType == "shop")
                        {
                            await unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""ScumServers"" SET ""ShopEnabled"" = {enableDisable} WHERE ""Id"" = {serverId}");
                        }
                        else if (switchType == "rank")
                        {
                            await unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""ScumServers"" SET ""RankEnabled"" = {enableDisable} WHERE ""Id"" = {serverId}");
                        }
                        else if (switchType == "task")
                        {
                            await unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""CustomTasks"" SET ""Enabled"" = {enableDisable} WHERE ""Id"" = {id}");
                        }

                        break;
                }

                foreach (var command in commands)
                    await botService.SendCommand(server.Id, command);

            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "CustomTaskJob Exception");
            }

        }
    }
}
