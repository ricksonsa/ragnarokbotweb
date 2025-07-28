using Microsoft.EntityFrameworkCore;
using Quartz;
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

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(SilenceExpireJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            var players = _unitOfWork.Players
                .Include(player => player.Silences)
                .Where(player =>
                    player.ScumServer != null
                    && player.SteamId64 != null
                    && player.ScumServer.Id == server.Id
                    && player.Silences.Any(silence => !silence.Indefinitely && silence.ExpirationDate.HasValue && silence.ExpirationDate.Value.Date < DateTime.UtcNow.Date));

            foreach (var player in players)
            {
                try
                {
                    _cacheService.GetFileChangeQueue(server.Id).Enqueue(new Models.FileChangeCommand
                    {
                        FileChangeMethod = Domain.Enums.EFileChangeMethod.RemoveLine,
                        FileChangeType = Domain.Enums.EFileChangeType.SilencedUsers,
                        Value = player.SteamId64!
                    });

                    _unitOfWork.Silences.Remove(player.RemoveSilence()!);
                    await _unitOfWork.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error trying to remove player silence -> {}", ex.Message);
                }
            }
        }
    }
}
