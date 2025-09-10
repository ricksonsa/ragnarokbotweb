using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class EconomyJob(
    ILogger<EconomyJob> logger,
    IScumServerRepository scumServerRepository
) : AbstractJob(scumServerRepository), IFtpJob
{
    public async Task Execute(long serverId, EFileType fileType)
    {
        try
        {
            logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            var server = await GetServerAsync(serverId);

            //var processor = new ScumFileProcessor(server, uow);

            //await foreach (var line in processor.UnreadFileLinesAsync(fileType, ftpService, context.CancellationToken))
            //{
            //    if (line.Contains("changed their name"))
            //    {
            //        var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line);
            //        await playerService.PlayerConnected(server, steamId64, scumId, changedName);
            //    }
            //}
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
            throw;
        }
    }
}