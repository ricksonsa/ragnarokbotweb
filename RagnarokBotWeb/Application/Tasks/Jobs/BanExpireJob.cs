using Microsoft.EntityFrameworkCore;
using Quartz;
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

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(BanExpireJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            var players = _unitOfWork.Players
                .Include(player => player.Bans)
                .Where(player =>
                    player.ScumServer != null
                    && player.SteamId64 != null
                    && player.ScumServer.Id == server.Id
                    && player.Bans.Any(ban => !ban.Indefinitely && ban.ExpirationDate.HasValue && ban.ExpirationDate.Value.Date < DateTime.UtcNow.Date));

            foreach (var player in players)
            {
                try
                {
                    _cacheService.GetFileChangeQueue(server.Id).Enqueue(new Models.FileChangeCommand
                    {
                        FileChangeMethod = Domain.Enums.EFileChangeMethod.RemoveLine,
                        FileChangeType = Domain.Enums.EFileChangeType.BannedUsers,
                        Value = player.SteamId64!
                    });

                    _unitOfWork.Bans.Remove(player.RemoveBan()!);
                    await _unitOfWork.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error trying to remove player ban -> {}", ex.Message);
                }
            }
        }
    }
}
