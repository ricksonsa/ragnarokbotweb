using Quartz;
using RagnarokBotWeb.Application.Handlers.ChangeFileHandler;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class FileChangeJob(
    ILogger<FileChangeJob> logger,
    IScumServerRepository scumServerRepository,
    IFtpService ftpService,
    IUnitOfWork unitOfWork,
    ICacheService cacheService
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {

        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

        try
        {
            var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
            if (cacheService.TryDequeueFileChangeCommand(server.Id, out var command))
            {
                try
                {
                    if (command is null) throw new ArgumentNullException("command");
                    var handler = new ChangeFileHandlerFactory(ftpService, unitOfWork).CreateAddRemoveLineHandler(command.FileChangeType);
                    await handler.Handle(command);
                    if (command.BotCommand is not null) cacheService.EnqueueCommand(command.ServerId, command.BotCommand);
                }
                catch (ArgumentNullException) { }
                catch (Exception ex)
                {
                    if (command!.Retries <= 5)
                    {
                        command.Retries += 1;
                        cacheService.EnqueueFileChangeCommand(command.ServerId, command);
                    }
                    logger.LogError(ex.Message);
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
}