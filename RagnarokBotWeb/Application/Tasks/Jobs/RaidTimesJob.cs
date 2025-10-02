using Newtonsoft.Json;
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
        IUnitOfWork uow,
        ICacheService cache
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(long serverId)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(serverId);
                if (server.Ftp is null) return;

                var processor = new ScumFileProcessor(server, uow);
                var data = await processor.DownloadRaidTimes(ftpService);
                var raidTime = JsonConvert.DeserializeObject<RaidTimes>(data);
                if (raidTime == null) return;
                cache.SetRaidTimes(server.Id, raidTime);
            }
            catch (ServerUncompliantException) { }
            catch (TenantDisabledException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
                throw;
            }

        }
    }
}
