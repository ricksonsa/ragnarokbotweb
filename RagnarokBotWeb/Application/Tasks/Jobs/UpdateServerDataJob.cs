using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class UpdateServerDataJob : AbstractJob, IJob
    {
        private readonly ILogger<UpdateServerDataJob> _logger;
        private readonly IServerService _serverService;

        public UpdateServerDataJob(
            ILogger<UpdateServerDataJob> logger,
            IServerService serverService,
            IScumServerRepository scumServerRepository
            ) : base(scumServerRepository)
        {
            _logger = logger;
            _serverService = serverService;
        }

        public async Task Execute(long serverId)
        {
            _logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(serverId, ftpRequired: true);
                await _serverService.UpdateServerData(server);
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
