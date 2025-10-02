using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class SilenceExpireJob : AbstractJob, IJob
    {
        private readonly ILogger<SilenceExpireJob> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public SilenceExpireJob(
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            ILogger<SilenceExpireJob> logger,
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
                var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);

                var players = await _unitOfWork.Players
                    .Include(player => player.Silences)
                    .Include(player => player.ScumServer)
                    .Where(player =>
                        player.ScumServer != null
                        && player.SteamId64 != null
                        && player.ScumServer.Id == server.Id
                        && player.Silences.Any(silence => !silence.Processed && !silence.Indefinitely && silence.ExpirationDate.HasValue && silence.ExpirationDate.Value.Date < DateTime.UtcNow.Date))
                    .ToListAsync();

                foreach (var player in players)
                {
                    try
                    {
                        _cacheService.EnqueueFileChangeCommand(server.Id, new Models.FileChangeCommand
                        {
                            FileChangeMethod = Domain.Enums.EFileChangeMethod.RemoveLine,
                            FileChangeType = Domain.Enums.EFileChangeType.SilencedUsers,
                            Value = player.SteamId64!,
                            ServerId = server.Id
                        });

                        var silence = player.RemoveSilence();
                        if (silence is null) return;
                        silence.Processed = true;
                        _unitOfWork.Silences.Update(silence);
                        await _unitOfWork.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error trying to remove player silence -> {Ex}", ex.Message);
                    }
                }
            }
            catch (ServerUncompliantException) { }
            catch (TenantDisabledException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
