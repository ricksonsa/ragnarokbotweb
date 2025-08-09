using Microsoft.EntityFrameworkCore;
using Quartz;
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
            _unitOfWork.CreateDbContext();
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            var server = await GetServerAsync(context, ftpRequired: true, validateSubscription: true);

            var players = _unitOfWork.Players
                .Include(player => player.Vips)
                .Where(player =>
                    player.ScumServer != null
                    && player.SteamId64 != null
                    && player.ScumServer.Id == server.Id
                    && player.Vips.Any(vip => !vip.Processed && !vip.Indefinitely && vip.ExpirationDate.HasValue && vip.ExpirationDate.Value.Date < DateTime.UtcNow.Date));

            foreach (var player in players)
            {
                try
                {
                    _cacheService.GetFileChangeQueue(server.Id).Enqueue(new Models.FileChangeCommand
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
    }
}
