using Quartz;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class WarzoneItemSpawnJob : AbstractJob, IJob
    {
        private readonly ILogger<WarzoneItemSpawnJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IBotRepository _botRepository;
        private readonly IWarzoneRepository _warzoneRepository;

        public WarzoneItemSpawnJob(
          ICacheService cacheService,
          IScumServerRepository scumServerRepository,
          IBotRepository botRepository,
          IWarzoneRepository warzoneRepository,
          ILogger<WarzoneItemSpawnJob> logger) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botRepository = botRepository;
            _warzoneRepository = warzoneRepository;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(WarzoneItemSpawnJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;
            var warzoneId = GetValueFromContext<long>(context, "warzone_id");
            if (warzoneId == 0) return;

            var warzone = await _warzoneRepository.FindByIdAsync(warzoneId);
            if (warzone == null) return;

            var warzoneItem = WarzoneItemSelector.SelectItem(warzone);
            var spawnPoint = WarzoneItemSelector.SelectSpawnPoint(warzone);

            var command = new BotCommand();

            command.Delivery($"\"{spawnPoint.Teleport.Coordinates}\"", warzoneItem.Item.Code, 1);

            if (!string.IsNullOrEmpty(warzone.DeliveryText))
            {
                command.Say(warzone.DeliveryText);
            }

            _cacheService.GetCommandQueue(server.Id).Enqueue(command);
        }
    }
}
