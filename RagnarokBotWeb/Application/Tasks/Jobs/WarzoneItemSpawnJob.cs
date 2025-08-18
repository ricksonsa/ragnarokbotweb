using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Exceptions;
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

            try
            {
                var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
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

                var command = new Shared.Models.BotCommand();

                // coordinates needs double quote
                var coordinates = spawnPoint.Teleport.Coordinates.Contains("{") ? $"\"{spawnPoint.Teleport.Coordinates}\"" : spawnPoint.Teleport.Coordinates;
                command.Delivery(coordinates, warzoneItem.Item.Code, 1, checkTargetOnline: false);

                if (!string.IsNullOrEmpty(warzone.DeliveryText))
                {
                    command.Say(warzone.ResolveDeliveryText(warzoneItem, spawnPoint));
                }

                await _botService.SendCommand(server.Id, command);
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                _logger.LogError("{Job} Exception -> {Ex} {Stack}", context.JobDetail.Key.Name, ex.Message, ex.StackTrace);
                throw;
            }

        }
    }
}
