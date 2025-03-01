using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;

namespace RagnarokBotWeb.HostedServices
{
    public class BotAliveHostedService : TimedHostedService
    {
        private readonly ILogger<BotAliveHostedService> _logger;
        private readonly IServiceProvider _services;

        public BotAliveHostedService(
            ILogger<BotAliveHostedService> logger,
            IServiceProvider services) : base(TimeSpan.FromMinutes(2))
        {
            _logger = logger;
            _services = services;
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered BotAliveHostedService->Process at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var botService = scope.ServiceProvider.GetRequiredService<IBotService>();
                    await botService.CheckBotState();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
