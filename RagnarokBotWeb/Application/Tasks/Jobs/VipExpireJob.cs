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
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(VipExpireJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            var players = _unitOfWork.Players
                .Include(player => player.Vips)
                .Where(player =>
                    player.ScumServer != null
                    && player.SteamId64 != null
                    && player.ScumServer.Id == server.Id
                    && player.Vips.Any(vip => !vip.Indefinitely && vip.ExpirationDate.HasValue && vip.ExpirationDate.Value.Date < DateTime.UtcNow.Date));

            foreach (var player in players)
            {
                try
                {
                    _cacheService.GetFileChangeQueue(server.Id).Enqueue(new Models.FileChangeCommand
                    {
                        FileChangeMethod = Domain.Enums.EFileChangeMethod.RemoveLine,
                        FileChangeType = Domain.Enums.EFileChangeType.Whitelist,
                        Value = player.SteamId64!
                    });

                    _unitOfWork.Vips.Remove(player.RemoveVip()!);
                    await _unitOfWork.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error trying to remove player vip -> {}", ex.Message);
                }
            }
        }
    }
}
