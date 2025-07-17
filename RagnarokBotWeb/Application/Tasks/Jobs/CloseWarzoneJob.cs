using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CloseWarzoneJob : AbstractJob, IJob
    {
        private readonly ILogger<CloseWarzoneJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IBotRepository _botRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchedulerFactory _schedulerFactory;

        public CloseWarzoneJob(
          ICacheService cacheService,
          IScumServerRepository scumServerRepository,
          IBotRepository botRepository,
          ISchedulerFactory schedulerFactory,
          ILogger<CloseWarzoneJob> logger,
          IUnitOfWork unitOfWork) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botRepository = botRepository;
            _schedulerFactory = schedulerFactory;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(CloseWarzoneJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;

            var warzoneId = GetValueFromContext<long>(context, "warzone_id");
            if (warzoneId == 0) return;

            var warzone = await _unitOfWork.Warzones.FirstOrDefaultAsync(warzone => warzone.Id == warzoneId);
            if (warzone == null) return;

            warzone.Stop();
            _unitOfWork.Warzones.Update(warzone);
            await _unitOfWork.SaveAsync();

            var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken);

            JobKey jobKey = new($"WarzoneItemSpawnJob({server.Id})");
            await scheduler.DeleteJob(jobKey);

            _logger.LogInformation("Warzone Id {} Closed for Server Id {} at: {time}", warzone.Id, server.Id, DateTimeOffset.Now);
        }
    }
}
