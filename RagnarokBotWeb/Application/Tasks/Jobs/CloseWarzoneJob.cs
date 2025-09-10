using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CloseWarzoneJob : AbstractJob, IWarzoneJob
    {
        private readonly ILogger<CloseWarzoneJob> _logger;
        private readonly IWarzoneService _warzoneService;

        public CloseWarzoneJob(
          IScumServerRepository scumServerRepository,
          ILogger<CloseWarzoneJob> logger,
          IWarzoneService warzoneService) : base(scumServerRepository)
        {
            _logger = logger;
            _warzoneService = warzoneService;
        }

        public async Task Execute(long serverId, long warzoneId)
        {
            _logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({warzoneId})", DateTimeOffset.Now);

            try
            {
                var warzone = await _warzoneService.FetchWarzoneById(warzoneId);
                var server = await GetServerAsync(serverId, ftpRequired: false);
                await _warzoneService.CloseWarzone(warzoneId);
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
