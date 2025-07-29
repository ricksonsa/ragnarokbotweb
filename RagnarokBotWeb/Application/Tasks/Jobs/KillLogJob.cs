using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class KillLogJob(
ILogger<KillLogJob> logger,
IScumServerRepository scumServerRepository,
IUnitOfWork unitOfWork,
IReaderPointerRepository readerPointerRepository,
IPlayerRepository playerRepository,
IReaderRepository readerRepository,
IFtpService ftpService
) : AbstractJob(scumServerRepository), IJob
{

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository);
            List<string> lines = [];

            await foreach (var line in processor.UnreadFileLinesAsync())
            {
                lines.Add(line);
            }
            using var enumerator = lines.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var first = enumerator.Current;
                if (!enumerator.MoveNext()) break;
                var second = enumerator.Current;

                if (string.IsNullOrWhiteSpace(first) && second.Contains("Game version")) continue;

                var players = await playerRepository.GetAllByServerId(server.Id);
                var kill = new KillLogParser(server).Parse(first, second);
                logger.Log(LogLevel.Information, "Adding new kill entry: {Killer} -> {Target}", kill.KillerName, kill.TargetName);
                unitOfWork.ScumServers.Attach(server);
                await unitOfWork.Kills.AddAsync(kill);
                await unitOfWork.SaveAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }
}