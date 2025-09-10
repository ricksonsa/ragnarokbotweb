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
    IBotService botService,
    IScumServerRepository scumServerRepository) : AbstractJob(scumServerRepository), ICustomTaskJob
    {
        public async Task Execute(long serverId, long customTaskId)
        {
            logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

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
