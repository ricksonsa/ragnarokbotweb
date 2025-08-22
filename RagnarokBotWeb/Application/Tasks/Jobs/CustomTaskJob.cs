using Quartz;
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
    IScumServerRepository scumServerRepository) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(context, validateSubscription: true);
                var customTaskId = GetValueFromContext<long>(context, "custom_task_id");
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

                        //case Domain.Enums.ECustomTaskType.ServerSettings:
                        // var command = new BotCommand();
                        //    foreach (var line in customTask.Commands.ToLines())
                        //        command = command.SayOrCommand(line);
                        //    cache.EnqueueCommand(server.Id, command);
                        //    break;
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
