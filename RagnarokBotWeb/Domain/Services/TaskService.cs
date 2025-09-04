using AutoMapper;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class TaskService : BaseService, ITaskService
    {
        private readonly ILogger<TaskService> _logger;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IMapper _mapper;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ICustomTaskRepository _customTaskRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public TaskService(
            IHttpContextAccessor httpContextAccessor,
            ISchedulerFactory schedulerFactory,
            IScumServerRepository scumServerRepository,
            ICustomTaskRepository customTaskRepository,
            ICacheService cacheService,
            ILogger<TaskService> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper) : base(httpContextAccessor)
        {
            _schedulerFactory = schedulerFactory;
            _scumServerRepository = scumServerRepository;
            _customTaskRepository = customTaskRepository;
            _cacheService = cacheService;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        private static ITrigger CronTrigger(string cron, ScumServer server, bool startNow = false)
        {
            var trigger = TriggerBuilder.Create()
                        .WithCronSchedule(cron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()));

            if (startNow)
                trigger.StartNow();

            return trigger.Build();
        }

        private static ITrigger OneMinTrigger(ScumServer server) => TriggerBuilder.Create()
                         .WithCronSchedule(AppSettingsStatic.OneMinCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                         .Build();

        private static ITrigger TwoMinTrigger(ScumServer server) => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.TwoMinCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                            .Build();

        private static ITrigger DefaultTrigger(ScumServer server) => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.DefaultCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                            .Build();

        private static ITrigger FiveMinTrigger(ScumServer server) => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.FiveMinCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                            .Build();

        private static ITrigger TenMinTrigger(ScumServer server) => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.TenMinCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                            .Build();

        private static ITrigger TenSecondsTrigger(ScumServer server) => TriggerBuilder.Create()
                            .WithCronSchedule(AppSettingsStatic.TenSecondsCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                            .Build();

        private static ITrigger ThirtySecondsTrigger(ScumServer server) => TriggerBuilder.Create()
                          .WithCronSchedule(AppSettingsStatic.ThirtySecondsCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                          .Build();

        private static ITrigger EveryDayTrigger(ScumServer server) => TriggerBuilder.Create()
                          .WithCronSchedule(AppSettingsStatic.EveryDayCron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(server.GetTimeZoneOrDefault()))
                          .Build();

        private async Task ScheduleServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            var job = JobBuilder.Create<OrderResetJob>()
                .WithIdentity(nameof(OrderResetJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger(server));

            job = JobBuilder.Create<ListPlayersJob>()
                .WithIdentity(nameof(ListPlayersJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, OneMinTrigger(server));

            job = JobBuilder.Create<ListSquadsJob>()
                .WithIdentity(nameof(ListSquadsJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0/15 * * * ?", server));

            job = JobBuilder.Create<ListFlagsJob>()
                .WithIdentity(nameof(ListFlagsJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 0/2 * * ?", server));

            job = JobBuilder.Create<CommandQueueProcessorJob>()
                .WithIdentity(nameof(CommandQueueProcessorJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, ThirtySecondsTrigger(server));

            job = JobBuilder.Create<OrderCommandJob>()
                .WithIdentity(nameof(OrderCommandJob), $"ServerJobs({server.Id})")
                 .UsingJobData("server_id", server.Id)
                 .Build();
            await scheduler.ScheduleJob(job, TenSecondsTrigger(server));

            job = JobBuilder.Create<UavClearJob>()
                .WithIdentity(nameof(UavClearJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, OneMinTrigger(server));

            job = JobBuilder.Create<WarzoneBootstartJob>()
                .WithIdentity(nameof(WarzoneBootstartJob), $"WarzoneJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0/10 * * * ?", server, startNow: true));

            job = JobBuilder.Create<BunkerStateJob>()
                .WithIdentity(nameof(BunkerStateJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * ? * *", server, startNow: false));

            job = JobBuilder.Create<KillRankJob>()
                .WithIdentity(nameof(KillRankJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * ? * *", server, startNow: false));

            job = JobBuilder.Create<LockpickRankJob>()
                .WithIdentity(nameof(LockpickRankJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * ? * *", server, startNow: false));

            job = JobBuilder.Create<LockpickRankDailyAwardJob>()
                .WithIdentity(nameof(LockpickRankDailyAwardJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 55 23 * * ? *", server, startNow: false));

            job = JobBuilder.Create<KillRankDailyAwardJob>()
                .WithIdentity(nameof(KillRankDailyAwardJob), $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 55 23 * * ? *", server, startNow: false));

            job = JobBuilder.Create<KillRankWeeklyAwardJob>()
              .WithIdentity(nameof(KillRankWeeklyAwardJob), $"ServerJobs({server.Id})")
              .UsingJobData("server_id", server.Id)
              .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 0 ? * SUN *", server, startNow: false));

            job = JobBuilder.Create<KillRankMonthlyAwardJob>()
              .WithIdentity(nameof(KillRankMonthlyAwardJob), $"ServerJobs({server.Id})")
              .UsingJobData("server_id", server.Id)
              .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 0 L * ? *", server, startNow: false));

            await AddPaydayJob(server);

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
            await scheduler.ScheduleJob(job, CronTrigger("0/10 * * * * ?", server));

            job = JobBuilder.Create<KillLogJob>()
                   .WithIdentity(nameof(KillLogJob), $"FtpJobs({server.Id})")
                   .UsingJobData("server_id", server.Id)
                   .UsingJobData("file_type", EFileType.Kill.ToString())
                   .Build();
            await scheduler.ScheduleJob(job, ThirtySecondsTrigger(server));

            job = JobBuilder.Create<EconomyJob>()
                .WithIdentity(nameof(EconomyJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Economy.ToString())
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger(server));

            job = JobBuilder.Create<LoginJob>()
                .WithIdentity(nameof(LoginJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Login.ToString())
                .Build();
            await scheduler.ScheduleJob(job, OneMinTrigger(server));

            job = JobBuilder.Create<GamePlayJob>()
                .WithIdentity(nameof(GamePlayJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Gameplay.ToString())
                .Build();
            await scheduler.ScheduleJob(job, ThirtySecondsTrigger(server));

            job = JobBuilder.Create<VipExpireJob>()
                .WithIdentity(nameof(VipExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger(server));

            job = JobBuilder.Create<BanExpireJob>()
                .WithIdentity(nameof(BanExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger(server));

            job = JobBuilder.Create<SilenceExpireJob>()
                .WithIdentity(nameof(SilenceExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger(server));

            job = JobBuilder.Create<DiscordRoleExpireJob>()
                .WithIdentity(nameof(DiscordRoleExpireJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TenMinTrigger(server));

            job = JobBuilder.Create<UpdateServerDataJob>()
                .WithIdentity(nameof(UpdateServerDataJob), $"FtpJobs({server.Id})")
               .UsingJobData("server_id", server.Id)
               .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 */6 * * ?", server));

            job = JobBuilder.Create<RaidTimesJob>()
                .WithIdentity(nameof(RaidTimesJob), $"FtpJobs({server.Id})")
               .UsingJobData("server_id", server.Id)
               .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0 * * * ?", server, startNow: true));

            job = JobBuilder.Create<FileChangeJob>()
                .WithIdentity(nameof(FileChangeJob), $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0/30 * * * * ?", server));

            _logger.LogInformation("Loaded ftp tasks for server id {Id}", server.Id);
        }

        public async Task AddPaydayJob(ScumServer server)
        {

            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(nameof(PaydayJob), $"ServerJobs({server.Id})");
            try
            {
                if (await scheduler.CheckExists(jobKey)) await DeleteJob(jobKey.Name, jobKey.Group);
            }
            catch (Exception) { }
            if (server.CoinAwardIntervalMinutes > 0)
            {
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes((int)server.CoinAwardIntervalMinutes)
                    .RepeatForever())
                    .Build();

                var job = JobBuilder.Create<PaydayJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("server_id", server.Id)
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
            }
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

        public async Task DeleteJob(string jobKey, string groupKey)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.DeleteJob(new JobKey(jobKey, groupKey));
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

        public async Task LoadCustomServersTasks(CancellationToken cancellationToken)
        {
            foreach (var task in await _customTaskRepository.GetServersEnabledCustomTasks())
            {
                await ScheduleCustomTask(task, cancellationToken);
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

        public async Task<Page<CustomTaskDto>> GetTaskPageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _customTaskRepository.GetPageByServerAndFilter(paginator, serverId!.Value, filter);
            return new Page<CustomTaskDto>(page.Content.Select(_mapper.Map<CustomTaskDto>), page.TotalPages, page.TotalElements, page.Number, page.Size);
        }

        public async Task<CustomTaskDto?> FetchTaskById(long id)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) throw new NotFoundException("Invalid server");
            ValidateServerOwner(server);

            var customTask = await _customTaskRepository.FindByIdAsync(id);
            return _mapper.Map<CustomTaskDto>(customTask);
        }

        public async Task<CustomTaskDto> CreateTask(CustomTaskDto customTaskDto)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) throw new NotFoundException("Invalid server");
            ValidateSubscription(server);

            var customTask = _mapper.Map<CustomTask>(customTaskDto);
            customTask.ScumServerId = server.Id;
            await _customTaskRepository.CreateOrUpdateAsync(customTask);
            await _customTaskRepository.SaveAsync();

            await ScheduleCustomTask(customTask);

            return _mapper.Map<CustomTaskDto>(customTask);
        }

        public async Task<CustomTaskDto> UpdateTask(long id, CustomTaskDto customTaskDto)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) throw new NotFoundException("Invalid server");
            ValidateSubscription(server);

            var customTask = await _customTaskRepository.FindByIdAsync(id);
            if (customTask == null) throw new NotFoundException("CustomTask not found");

            customTask = _mapper.Map(customTaskDto, customTask);
            customTask.ScumServerId = serverId!.Value;
            await _customTaskRepository.CreateOrUpdateAsync(customTask);
            await _customTaskRepository.SaveAsync();

            await ScheduleCustomTask(customTask);

            return _mapper.Map<CustomTaskDto>(customTask);
        }

        public async Task<CustomTaskDto> DeleteCustomTask(long id)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) throw new NotFoundException("Invalid server");
            ValidateSubscription(server);

            var customTask = await _customTaskRepository.FindByIdAsync(id);
            if (customTask == null) throw new NotFoundException("CustomTask not found");

            var key = $"{nameof(CustomTaskJob)}({customTask.Id})";
            var group = $"CustomTasks({customTask.ScumServerId})";

            _customTaskRepository.Delete(customTask);
            await _customTaskRepository.SaveAsync();

            try
            {
                if (await scheduler.CheckExists(new JobKey(key, group))) await DeleteJob(key, group);
            }
            catch (Exception) { }

            return _mapper.Map<CustomTaskDto>(customTask);
        }

        public async Task ScheduleCustomTask(CustomTask customTask, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var key = $"{nameof(CustomTaskJob)}({customTask.Id})";
            var group = $"CustomTasks({customTask.ScumServerId})";

            var customTaskJob = JobBuilder.Create<CustomTaskJob>()
                .WithIdentity(key, group)
                .UsingJobData("server_id", customTask.ScumServerId!.Value)
                .UsingJobData("custom_task_id", customTask.Id)
                .Build();

            try
            {
                if (await scheduler.CheckExists(new JobKey(key, group), cancellationToken)) await DeleteJob(key, group);
            }
            catch (Exception) { }

            var trigger = TriggerBuilder.Create().WithCronSchedule(customTask.Cron, cronScheduleBuilder =>
                                cronScheduleBuilder.InTimeZone(customTask.ScumServer.GetTimeZoneOrDefault()));

            await scheduler.ScheduleJob(customTaskJob, trigger.Build(), cancellationToken);
        }
    }
}
