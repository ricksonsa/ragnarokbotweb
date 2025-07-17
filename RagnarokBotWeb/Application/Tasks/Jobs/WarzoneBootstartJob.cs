using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class WarzoneBootstartJob : AbstractJob, IJob
    {
        private readonly ILogger<WarzoneBootstartJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IBotRepository _botRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchedulerFactory _schedulerFactory;

        public WarzoneBootstartJob(
          ICacheService cacheService,
          IScumServerRepository scumServerRepository,
          IBotRepository botRepository,
          ISchedulerFactory schedulerFactory,
          ILogger<WarzoneBootstartJob> logger,
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
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(WarzoneBootstartJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;

            var warzones = await _unitOfWork.Warzones
                .Include(warzone => warzone.ScumServer)
                .Where(warzone => warzone.Enabled && warzone.ScumServer.Id == server.Id)
                .ToListAsync();

            var warzone = new WarzoneSelector(_cacheService, server).Select(warzones);

            if (warzone is null) return;

            if (!warzone.IsRunning)
            {
                warzone.Run();
                _unitOfWork.Warzones.Update(warzone);
                await _unitOfWork.SaveAsync();
            }

            var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken);
            var closeWarzoneJob = JobBuilder.Create<CloseWarzoneJob>()
               .WithIdentity($"CloseWarzoneJob({server.Id})")
               .UsingJobData("server_id", server.Id)
               .UsingJobData("warzone_id", warzone.Id)
               .Build();

            ITrigger warzoneClosingTrigger = TriggerBuilder.Create()
                .WithIdentity("CloseWarzoneJobTrigger", server.Id.ToString())
                .StartAt(new DateTimeOffset(warzone.StopAt!.Value))
                .WithSimpleSchedule(x => x.WithRepeatCount(0)) // only once
                .Build();

            await scheduler.ScheduleJob(closeWarzoneJob, warzoneClosingTrigger);

            var warzoneItemSpawnJob = JobBuilder.Create<WarzoneItemSpawnJob>()
               .WithIdentity($"WarzoneItemSpawnJob({server.Id})")
               .UsingJobData("server_id", server.Id)
               .UsingJobData("warzone_id", warzone.Id)
               .Build();

            ITrigger warzoneItemSpawnJobTrigger = TriggerBuilder.Create()
               .WithIdentity("WarzoneItemSpawnJobTrigger", server.Id.ToString())
               .StartNow() // Start immediately
               .WithSimpleSchedule(x => x
                   .WithIntervalInMinutes((int)warzone.ItemSpawnInterval)
                   .RepeatForever())
               .Build();

            await scheduler.ScheduleJob(warzoneItemSpawnJob, warzoneItemSpawnJobTrigger);

            if (!string.IsNullOrEmpty(warzone.StartMessage))
            {
                var command = new BotCommand();
                command.Announce(warzone.StartMessage);
                _cacheService.GetCommandQueue(server.Id).Enqueue(command);
            }

            _logger.LogInformation("Warzone Id {} Opened for Server Id {} at: {time}", warzone.Id, server.Id, DateTimeOffset.Now);

        }
    }
}
