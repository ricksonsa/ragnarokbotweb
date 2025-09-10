using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class WarzoneBootstartJob : AbstractJob, IJob
    {
        private readonly ILogger<WarzoneBootstartJob> _logger;
        private readonly IBotService _botService;
        private readonly IWarzoneService _warzoneService;

        public WarzoneBootstartJob(
          IScumServerRepository scumServerRepository,
          IBotService botService,
          IWarzoneService warzoneService,
          ILogger<WarzoneBootstartJob> logger) : base(scumServerRepository)
        {
            _botService = botService;
            _warzoneService = warzoneService;
            _logger = logger;
        }

        public async Task Execute(long serverId)
        {
            _logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            try
            {
                var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);

                if (!_botService.IsBotOnline(server.Id)) return;

                await _warzoneService.OpenWarzone(server);
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
