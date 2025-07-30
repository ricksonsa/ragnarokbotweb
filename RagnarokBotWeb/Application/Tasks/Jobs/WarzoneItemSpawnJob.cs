using Microsoft.EntityFrameworkCore;
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
        private readonly IBotService _botService;
        private readonly IUnitOfWork _unitOfWork;

        public WarzoneItemSpawnJob(
          ICacheService cacheService,
          IScumServerRepository scumServerRepository,
          IBotService botService,
          ILogger<WarzoneItemSpawnJob> logger,
          IUnitOfWork unitOfWork) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botService = botService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if (!_botService.IsBotOnline(server.Id)) return;
            var warzoneId = GetValueFromContext<long>(context, "warzone_id");
            if (warzoneId == 0) return;

            var warzone = await _unitOfWork.Warzones
                .Include(warzone => warzone.ScumServer)
                .Include(warzone => warzone.WarzoneItems)
                    .ThenInclude(warzone => warzone.Item)
                .Include(warzone => warzone.SpawnPoints)
                    .ThenInclude(warzone => warzone.Teleport)
                .FirstOrDefaultAsync(warzone => warzone.Id == warzoneId);

            if (warzone == null) return;

            var warzoneItem = WarzoneRandomSelector.SelectItem(warzone);
            var spawnPoint = WarzoneRandomSelector.SelectSpawnPoint(warzone);

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
