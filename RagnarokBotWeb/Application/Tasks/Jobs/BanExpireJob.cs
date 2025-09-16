using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class BanExpireJob : AbstractJob, IJob
    {
        private readonly ILogger<BanExpireJob> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public BanExpireJob(
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            ILogger<BanExpireJob> logger,
            ICacheService cacheService) : base(scumServerRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task Execute(long serverId)
        {
            try
            {
                var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);
                _logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

                var players = await _unitOfWork.Players
                    .Include(player => player.ScumServer)
                    .Include(player => player.Bans)
                    .Where(player =>
                        player.ScumServer != null
                        && player.SteamId64 != null
                        && player.ScumServer.Id == server.Id
                        && player.Bans.Any(ban => !ban.Processed && !ban.Indefinitely && ban.ExpirationDate.HasValue && ban.ExpirationDate.Value.Date < DateTime.UtcNow.Date))
                    .ToListAsync();

                foreach (var player in players)
                {
                    try
                    {
                        _cacheService.EnqueueFileChangeCommand(server.Id, new Models.FileChangeCommand
                        {
                            FileChangeMethod = Domain.Enums.EFileChangeMethod.RemoveLine,
                            FileChangeType = Domain.Enums.EFileChangeType.BannedUsers,
                            Value = player.SteamId64!,
                            ServerId = server.Id
                        });

                        var ban = player.RemoveBan();
                        if (ban is null) return;
                        ban.Processed = true;
                        _unitOfWork.Bans.Update(ban);
                        await _unitOfWork.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error trying to remove player ban -> {Ex}", ex.Message);
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
