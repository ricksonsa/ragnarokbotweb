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
            if (cacheService.GetFileChangeQueue(server.Id).TryDequeue(out var command))
            {
                try
                {
                    var handler = new ChangeFileHandlerFactory(ftpService, unitOfWork).CreateAddRemoveLineHandler(command.FileChangeType);
                    await handler.Handle(command);
                    if (command.BotCommand is not null) cacheService.GetCommandQueue(command.ServerId).Enqueue(command.BotCommand);
                }
                catch (Exception ex)
                {
                    if (command.Retries <= 5)
                    {
                        command.Retries += 1;
                        cacheService.GetFileChangeQueue(command.ServerId).Enqueue(command);
                    }
                    logger.LogError(ex.Message);
                }
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception)
        {

            throw;
        }

    }
}