using Newtonsoft.Json;
using Quartz;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class RaidTimesJob(
        ILogger<RaidTimesJob> logger,
        IScumServerRepository scumServerRepository,
        IFtpService ftpService,
        ICacheService cache
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(context);
                if (server.Ftp is null) return;

                var processor = new ScumFileProcessor(server);
                var data = await processor.DownloadRaidTimes(ftpService);
                var raidTime = JsonConvert.DeserializeObject<RaidTimes>(data);
                if (raidTime == null) return;
                cache.SetRaidTimes(server.Id, raidTime);
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
