using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class EconomyJob(
    ILogger<EconomyJob> logger,
    IScumServerRepository scumServerRepository,
    IPlayerService playerService,
    IFtpService ftpService,
    IUnitOfWork uow
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(server, uow);

            await foreach (var line in processor.UnreadFileLinesAsync(fileType, ftpService, context.CancellationToken))
            {
                if (line.Contains("changed their name"))
                {
                    var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line);
                    await playerService.PlayerConnected(server, steamId64, scumId, changedName);
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