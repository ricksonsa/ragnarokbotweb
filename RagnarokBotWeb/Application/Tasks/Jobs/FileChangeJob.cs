using Quartz;
using RagnarokBotWeb.Application.Handlers.ChangeFileHandler;
using RagnarokBotWeb.Application.Models;
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
        try
        {
            logger.LogInformation("Triggered {}->Execute at: {time}", nameof(FileChangeJob), DateTimeOffset.Now);
            var server = await GetServerAsync(context, ftpRequired: false);

            FileChangeCommand? command = cacheService.GetFileChangeQueue(server.Id).Dequeue();
            if (command == null) return;

            var handler = new ChangeFileHandlerFactory(ftpService, unitOfWork).CreateAddRemoveLineHandler(command.FileChangeType);
            await handler.Handle(command);

            if (command.BotCommand is not null) cacheService.GetCommandQueue(command.ServerId).Enqueue(command.BotCommand);

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }
}