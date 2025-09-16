using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class VipExpireJob : AbstractJob, IJob
    {
        private readonly ILogger<VipExpireJob> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public VipExpireJob(
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            ILogger<VipExpireJob> logger,
            ICacheService cacheService) : base(scumServerRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task Execute(long serverId)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(serverId, ftpRequired: true, validateSubscription: true);

                var players = await _unitOfWork.Players
                    .Include(player => player.Vips)
                    .Include(player => player.ScumServer)
                    .Where(player =>
                        player.ScumServer != null
                        && player.SteamId64 != null
                        && player.ScumServer.Id == server.Id
                        && player.Vips.Any(vip => !vip.Processed
                            && !vip.Indefinitely
                            && vip.ExpirationDate.HasValue && DateTime.UtcNow.Date > vip.ExpirationDate.Value.Date)
                        )
                    .ToListAsync();

                foreach (var player in players)
                {
                    try
                    {
                        _cacheService.EnqueueFileChangeCommand(server.Id, new Models.FileChangeCommand
                        {
                            FileChangeMethod = Domain.Enums.EFileChangeMethod.RemoveLine,
                            FileChangeType = Domain.Enums.EFileChangeType.Whitelist,
                            Value = player.SteamId64!,
                            ServerId = server.Id
                        });

                        var vip = player.RemoveVip();
                        if (vip is null) return;
                        vip.Processed = true;
                        _unitOfWork.Vips.Update(vip);
                        await _unitOfWork.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error trying to remove player vip -> {Ex}", ex.Message);
                    }
                }
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
