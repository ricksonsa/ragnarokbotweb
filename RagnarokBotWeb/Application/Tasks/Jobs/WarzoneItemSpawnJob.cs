using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class WarzoneItemSpawnJob : AbstractJob, IWarzoneJob
    {
        private readonly ILogger<WarzoneItemSpawnJob> _logger;
        private readonly IBotService _botService;
        private readonly IUnitOfWork _unitOfWork;

        public WarzoneItemSpawnJob(
          IScumServerRepository scumServerRepository,
          IBotService botService,
          ILogger<WarzoneItemSpawnJob> logger,
          IUnitOfWork unitOfWork) : base(scumServerRepository)
        {
            _botService = botService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(long serverId, long warzoneId)
        {
            _logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({warzoneId})", DateTimeOffset.Now);

            try
            {
                var warzone = await _unitOfWork.Warzones
                   .Include(warzone => warzone.ScumServer)
                   .Include(warzone => warzone.WarzoneItems)
                       .ThenInclude(warzone => warzone.Item)
                   .Include(warzone => warzone.SpawnPoints)
                       .ThenInclude(warzone => warzone.Teleport)
                   .FirstOrDefaultAsync(warzone => warzone.Id == warzoneId);

                if (warzone == null) return;

                var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);
                if (!_botService.IsBotOnline(server.Id)) return;

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
            catch (TenantDisabledException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({warzoneId})");
                throw;
            }

        }
    }
}
