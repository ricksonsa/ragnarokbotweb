using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class TaskService : ITaskService
    {
        private readonly ILogger<TaskService> _logger;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public TaskService(
            ISchedulerFactory schedulerFactory,
            IScumServerRepository scumServerRepository,
            ICacheService cacheService,
            ILogger<TaskService> logger,
            IUnitOfWork unitOfWork)
        {
            _schedulerFactory = schedulerFactory;
            _scumServerRepository = scumServerRepository;
            _cacheService = cacheService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        private static ITrigger CronTrigger(string cron, bool startNow = false)
        {
            var trigger = TriggerBuilder.Create()
                        .WithCronSchedule(cron);

            if (startNow)
                trigger.StartNow();

            return trigger.Build();
        }

        private static ITrigger OneMinTrigger() => TriggerBuilder.Create()
                         .WithCronSchedule(AppSettingsStatic.OneMinCron)
                         .Build();

        private static ITrigger TwoMinTrigger() => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.TwoMinCron)
                            .Build();

        private static ITrigger DefaultTrigger() => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.DefaultCron)
                            .Build();

        private static ITrigger FiveMinTrigger() => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.FiveMinCron)
                            .Build();

        private static ITrigger TenMinTrigger() => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.TenMinCron)
                            .Build();

        private static ITrigger TenSecondsTrigger() => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.TenSecondsCron)
                            .Build();

        private static ITrigger ThirtySecondsTrigger() => TriggerBuilder.Create()
                          .WithCronSchedule(AppSettingsStatic.ThirtySecondsCron)
                          .Build();

        private static ITrigger EveryDayTrigger() => TriggerBuilder.Create()
                          .WithCronSchedule(AppSettingsStatic.EveryDayCron)
                          .Build();

        private async Task ScheduleServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            var job = JobBuilder.Create<BotAliveJob>()
                .WithIdentity(nameof(BotAliveJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, OneMinTrigger());

            job = JobBuilder.Create<OrderResetJob>()
                .WithIdentity(nameof(OrderResetJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<ListPlayersJob>()
                .WithIdentity(nameof(ListPlayersJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, OneMinTrigger());

            job = JobBuilder.Create<ListSquadsJob>()
                .WithIdentity(nameof(ListSquadsJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0/15 * * * ?"));

            job = JobBuilder.Create<ListFlagsJob>()
                .WithIdentity(nameof(ListFlagsJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 0/2 * * ?"));

            job = JobBuilder.Create<OrderCommandJob>()
                .WithIdentity(nameof(OrderCommandJob), $"ServerJobs({server.Id})")
                 .UsingJobData("server_id", server.Id)
                 .Build();
            await scheduler.ScheduleJob(job, TenSecondsTrigger());

            job = JobBuilder.Create<UavClearJob>()
                .WithIdentity(nameof(UavClearJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, OneMinTrigger());

            job = JobBuilder.Create<WarzoneBootstartJob>()
                .WithIdentity(nameof(WarzoneBootstartJob), $"WarzoneJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0/10 * * * ?", startNow: true));

            job = JobBuilder.Create<BunkerStateJob>()
                .WithIdentity(nameof(BunkerStateJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * ? * *", startNow: true));

            job = JobBuilder.Create<KillRankJob>()
                .WithIdentity(nameof(KillRankJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * ? * *", startNow: true));

            job = JobBuilder.Create<LockpickRankJob>()
                .WithIdentity(nameof(LockpickRankJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * ? * *", startNow: true));

            job = JobBuilder.Create<PaydayJob>()
                .WithIdentity(nameof(PaydayJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0/30 * * * ?"));

            _logger.LogInformation("Loaded server tasks for server id {Id}", server.Id);
        }

        private async Task ScheduleFtpServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            var job = JobBuilder.Create<ChatJob>()
                .WithIdentity(nameof(ChatJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Chat.ToString())
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0/10 * * * * ?"));

            job = JobBuilder.Create<KillLogJob>()
                   .WithIdentity(nameof(KillLogJob), $"FtpJobs({server.Id})")
                   .UsingJobData("server_id", server.Id)
                   .UsingJobData("file_type", EFileType.Kill.ToString())
                   .Build();
            await scheduler.ScheduleJob(job, ThirtySecondsTrigger());

            job = JobBuilder.Create<EconomyJob>()
                .WithIdentity(nameof(EconomyJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Economy.ToString())
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<GamePlayJob>()
                .WithIdentity(nameof(GamePlayJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Gameplay.ToString())
                .Build();
            await scheduler.ScheduleJob(job, ThirtySecondsTrigger());

            job = JobBuilder.Create<VipExpireJob>()
                .WithIdentity(nameof(VipExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger());

            job = JobBuilder.Create<BanExpireJob>()
                .WithIdentity(nameof(BanExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger());

            job = JobBuilder.Create<SilenceExpireJob>()
                .WithIdentity(nameof(SilenceExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger());

            job = JobBuilder.Create<DiscordRoleExpireJob>()
                .WithIdentity(nameof(DiscordRoleExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger());

            job = JobBuilder.Create<UpdateServerDataJob>()
                .WithIdentity(nameof(UpdateServerDataJob), $"FtpJobs({server.Id})")
               .UsingJobData("server_id", server.Id)
               .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 */6 * * ?"));

            job = JobBuilder.Create<RaidTimesJob>()
                .WithIdentity(nameof(RaidTimesJob), $"FtpJobs({server.Id})")
               .UsingJobData("server_id", server.Id)
               .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * * * ?", startNow: true));

            job = JobBuilder.Create<FileChangeJob>()
                .WithIdentity(nameof(FileChangeJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0/30 * * * * ?"));

            _logger.LogInformation("Loaded ftp tasks for server id {Id}", server.Id);
        }

        public async Task NewServerAddedAsync(ScumServer server)
        {
            await ScheduleServerTasks(server);
            _cacheService.AddServers([server]);
        }

        public async Task FtpConfigAddedAsync(ScumServer server)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            try
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals($"FtpJobs({server.Id})"));
                if (jobKeys.Any()) await scheduler.DeleteJobs(jobKeys.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            await ScheduleFtpServerTasks(server);
            _cacheService.AddServers([server]);
        }

        public async Task DeleteJob(string jobKey)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.DeleteJob(new JobKey(jobKey));
        }

        public async Task LoadAllServersTasks(CancellationToken cancellationToken)
        {
            var servers = await _scumServerRepository.FindActive();
            _cacheService.AddServers(servers);
            foreach (var server in await _scumServerRepository.GetActiveServersWithFtp())
            {
                await ScheduleServerTasks(server, cancellationToken);
            }
        }

        public async Task LoadFtpAllServersTasks(CancellationToken cancellationToken)
        {
            foreach (var server in await _scumServerRepository.GetActiveServersWithFtp())
            {
                await ScheduleFtpServerTasks(server, cancellationToken);
            }
        }

        public async Task LoadRaidTimes(CancellationToken stoppingToken)
        {

            foreach (var server in await _scumServerRepository.FindActive())
            {
                var processor = new ScumFileProcessor(server, _unitOfWork);

                try
                {
                    var raidTimeString = await processor.ReadLocalRaidTimesAsync(stoppingToken);
                    if (raidTimeString != null)
                    {
                        var raidTimes = JsonConvert.DeserializeObject<RaidTimes>(raidTimeString);
                        if (raidTimes != null) _cacheService.SetRaidTimes(server.Id, raidTimes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("LoadRaidTimesHostedService error reading server initial files -> {Ex}", ex.Message);
                }
            }
        }

        public async Task LoadSquads(CancellationToken stoppingToken)
        {

            foreach (var server in await _scumServerRepository.FindActive())
            {
                var processor = new ScumFileProcessor(server, _unitOfWork);

                try
                {
                    var squadListString = await processor.ReadSquadListAsync(stoppingToken);
                    if (squadListString != null)
                    {
                        var squads = JsonConvert.DeserializeObject<List<Shared.Models.ScumSquad>>(squadListString);
                        if (squads != null) _cacheService.SetSquads(server.Id, squads);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("LoadSquadsHostedService error reading server initial files -> {Ex}", ex.Message);
                }
            }
        }

        public async Task LoadFlags(CancellationToken stoppingToken)
        {

            foreach (var server in await _scumServerRepository.FindActive())
            {
                var processor = new ScumFileProcessor(server, _unitOfWork);

                try
                {
                    var flagListString = await processor.ReadFlagListAsync(stoppingToken);
                    if (flagListString != null)
                    {
                        var flags = JsonConvert.DeserializeObject<List<Shared.Models.ScumFlag>>(flagListString);
                        if (flags != null) _cacheService.SetFlags(server.Id, flags);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("LoadFlagsHostedService error reading server initial files -> {Ex}", ex.Message);
                }
            }
        }

        public async Task CreateWarzoneJobs(ScumServer server, Warzone warzone)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var closeWarzoneJob = JobBuilder.Create<CloseWarzoneJob>()
                .WithIdentity(nameof(CloseWarzoneJob), $"WarzoneJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("warzone_id", warzone.Id)
                .Build();

            ITrigger warzoneClosingTrigger = TriggerBuilder.Create()
                .StartAt(new DateTimeOffset(warzone.StopAt!.Value))
                .WithSimpleSchedule(x => x.WithRepeatCount(0)) // only once
                .Build();

            await scheduler.ScheduleJob(closeWarzoneJob, warzoneClosingTrigger);

            var warzoneItemSpawnJob = JobBuilder.Create<WarzoneItemSpawnJob>()
                .WithIdentity(nameof(WarzoneItemSpawnJob), $"WarzoneJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("warzone_id", warzone.Id)
                .Build();

            ITrigger warzoneItemSpawnJobTrigger = TriggerBuilder.Create()
                .StartNow() // Start immediately
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes((int)warzone.ItemSpawnInterval)
                .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(warzoneItemSpawnJob, warzoneItemSpawnJobTrigger);
        }

        public async Task TriggerJob(string jobId, string groupId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey(jobId, groupId);

            // Trigger the job immediately
            await scheduler.TriggerJob(jobKey);
        }

        public async Task<List<JobModel>> ListJobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            List<JobModel> jobs = [];

            // Get all job group names
            var jobGroupNames = await scheduler.GetJobGroupNames();

            foreach (var group in jobGroupNames)
            {
                // Get all job keys in the group
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));

                foreach (var jobKey in jobKeys)
                {
                    var job = new JobModel
                    {
                        JobID = jobKey.Name,
                        GroupID = jobKey.Group
                    };

                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    foreach (var trigger in triggers)
                    {
                        job.NextFireTime = trigger.GetNextFireTimeUtc();
                    }
                    jobs.Add(job);
                }
            }
            return jobs;
        }

        public async Task DeleteWarzoneJobs(ScumServer server)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            try
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals($"WarzoneJobs({server.Id})"));

                if (jobKeys.Any())
                {
                    await scheduler.DeleteJobs(jobKeys.ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
