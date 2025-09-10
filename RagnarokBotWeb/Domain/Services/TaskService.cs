using AutoMapper;
using Hangfire;
using Hangfire.Storage;
using Newtonsoft.Json;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Tasks.Jobs;
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
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IMapper _mapper;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ICustomTaskRepository _customTaskRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IServiceProvider _serviceProvider;

        // Store job IDs for cleanup
        private readonly Dictionary<long, List<string>> _serverJobIds = new();
        private readonly Dictionary<long, List<string>> _ftpJobIds = new();

        public TaskService(
            IHttpContextAccessor httpContextAccessor,
            IRecurringJobManager recurringJobManager,
            IBackgroundJobClient backgroundJobClient,
            IScumServerRepository scumServerRepository,
            ICustomTaskRepository customTaskRepository,
            ICacheService cacheService,
            ILogger<TaskService> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IServiceProvider serviceProvider) : base(httpContextAccessor)
        {
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
            _scumServerRepository = scumServerRepository;
            _customTaskRepository = customTaskRepository;
            _cacheService = cacheService;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        private static string GetCronWithTimeZone(string cron, TimeZoneInfo timeZone)
        {
            // Hangfire uses UTC by default, but we can handle timezone conversion in the job itself
            // or use Hangfire.Cron helper methods
            return cron;
        }

        private void CleanupServerJobs(long serverId, string jobType)
        {
            try
            {
                var jobIds = jobType == "ServerJobs"
                    ? _serverJobIds.GetValueOrDefault(serverId, new List<string>())
                    : _ftpJobIds.GetValueOrDefault(serverId, new List<string>());

                foreach (var jobId in jobIds)
                {
                    try
                    {
                        _recurringJobManager.RemoveIfExists(jobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to remove job {JobId}", jobId);
                    }
                }

                if (jobType == "ServerJobs")
                    _serverJobIds[serverId] = new List<string>();
                else
                    _ftpJobIds[serverId] = new List<string>();

                _logger.LogInformation("Cleaned up existing {JobType} jobs for server {ServerId}",
                    jobType, serverId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup existing jobs for server {ServerId} in type {JobType}",
                    serverId, jobType);
            }
        }

        private void ScheduleServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting to schedule server tasks for server id {ServerId}", server.Id);

                // Clean up existing jobs for this server first
                CleanupServerJobs(server.Id, "ServerJobs");

                var jobIds = new List<string>();

                // OrderResetJob - every 5 minutes
                var orderResetJobId = $"OrderResetJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<OrderResetJob>(
                    orderResetJobId,
                    job => job.Execute(server.Id),
                    "*/5 * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(orderResetJobId);

                // CommandQueueProcessorJob - every 1 min
                var commandQueueJobId = $"CommandQueueProcessorJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<CommandQueueProcessorJob>(
                    commandQueueJobId,
                    job => job.Execute(server.Id),
                   "* * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(commandQueueJobId);

                // UavClearJob - every minute
                var uavClearJobId = $"UavClearJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<UavClearJob>(
                    uavClearJobId,
                    job => job.Execute(server.Id),
                    "* * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(uavClearJobId);

                // BunkerStateJob - every hour
                var bunkerStateJobId = $"BunkerStateJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<BunkerStateJob>(
                    bunkerStateJobId,
                    job => job.Execute(server.Id),
                    "0 * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(bunkerStateJobId);

                // KillRankJob - every hour
                var killRankJobId = $"KillRankJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<KillRankJob>(
                    killRankJobId,
                    job => job.Execute(server.Id),
                    "0 * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(killRankJobId);

                // LockpickRankJob - every hour
                var lockpickRankJobId = $"LockpickRankJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<LockpickRankJob>(
                    lockpickRankJobId,
                    job => job.Execute(server.Id),
                    "0 * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(lockpickRankJobId);

                // LockpickRankDailyAwardJob - daily at 23:55
                var lockpickDailyAwardJobId = $"LockpickRankDailyAwardJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<LockpickRankDailyAwardJob>(
                    lockpickDailyAwardJobId,
                    job => job.Execute(server.Id),
                    "55 23 * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(lockpickDailyAwardJobId);

                // KillRankDailyAwardJob - daily at 23:55
                var killDailyAwardJobId = $"KillRankDailyAwardJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<KillRankDailyAwardJob>(
                    killDailyAwardJobId,
                    job => job.Execute(server.Id),
                    "55 23 * * *", // Daily at 23:55
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(killDailyAwardJobId);

                // KillRankWeeklyAwardJob - weekly on Sunday
                var killWeeklyAwardJobId = $"KillRankWeeklyAwardJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<KillRankWeeklyAwardJob>(
                    killWeeklyAwardJobId,
                    job => job.Execute(server.Id),
                    "0 0 * * 0", // Weekly on Sunday at midnight
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(killWeeklyAwardJobId);

                // KillRankMonthlyAwardJob - monthly on last day
                var killMonthlyAwardJobId = $"KillRankMonthlyAwardJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<KillRankMonthlyAwardJob>(
                    killMonthlyAwardJobId,
                    job => job.Execute(server.Id),
                    "0 0 28-31 * *", // Last few days of month (Hangfire will handle)
                     new RecurringJobOptions
                     {
                         TimeZone = server.GetTimeZoneOrDefault()
                     });
                jobIds.Add(killMonthlyAwardJobId);

                // WarzoneBootstartJob - every 10 minutes
                var warzoneBootstartJobId = $"WarzoneBootstartJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<WarzoneBootstartJob>(
                    warzoneBootstartJobId,
                    job => job.Execute(server.Id),
                    "*/10 * * * *", // Every 10 minutes
                     new RecurringJobOptions
                     {
                         TimeZone = server.GetTimeZoneOrDefault()
                     });
                jobIds.Add(warzoneBootstartJobId);

                _serverJobIds[server.Id] = jobIds;

                BackgroundJob.Schedule<PaydayJob>(
                   job => job.Execute(server.Id),
                   TimeSpan.FromMinutes(server.CoinAwardIntervalMinutes)
                );

                _logger.LogInformation("Successfully loaded {JobCount} server tasks for server id {ServerId}",
                    jobIds.Count + 1, server.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule server tasks for server id {ServerId}", server.Id);
                throw;
            }
        }

        private void ScheduleFtpServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting to schedule FTP tasks for server id {ServerId}", server.Id);

                // Clean up existing FTP jobs for this server first
                CleanupServerJobs(server.Id, "FtpJobs");

                var jobIds = new List<string>();

                // KillLogJob - every 1 minute
                var killLogJobId = $"KillLogJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<KillLogJob>(
                    killLogJobId,
                    job => job.Execute(server.Id, EFileType.Kill),
                    "* * * * *",
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    }); // Every 1 minute
                jobIds.Add(killLogJobId);

                // EconomyJob - every 5 minutes
                var economyJobId = $"EconomyJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<EconomyJob>(
                    economyJobId,
                    job => job.Execute(server.Id, EFileType.Economy),
                    "*/5 * * * *", // Every 5 minutes
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(economyJobId);

                // LoginJob - every minute
                var loginJobId = $"LoginJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<LoginJob>(
                    loginJobId,
                    job => job.Execute(server.Id, EFileType.Login),
                    "* * * * *", // Every minute
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(loginJobId);

                // GamePlayJob - every 1 min
                var gamePlayJobId = $"GamePlayJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<GamePlayJob>(
                    gamePlayJobId,
                    job => job.Execute(server.Id, EFileType.Gameplay),
                    "* * * * *", // Every minute
                     new RecurringJobOptions
                     {
                         TimeZone = server.GetTimeZoneOrDefault()
                     });
                jobIds.Add(gamePlayJobId);

                // VipExpireJob - every 10 minutes
                var vipExpireJobId = $"VipExpireJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<VipExpireJob>(
                    vipExpireJobId,
                    job => job.Execute(server.Id),
                    "*/10 * * * *", // Every 10 minutes
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(vipExpireJobId);

                // BanExpireJob - every 10 minutes
                var banExpireJobId = $"BanExpireJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<BanExpireJob>(
                    banExpireJobId,
                    job => job.Execute(server.Id),
                    "*/10 * * * *", // Every 10 minutes
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(banExpireJobId);

                // SilenceExpireJob - every 10 minutes
                var silenceExpireJobId = $"SilenceExpireJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<SilenceExpireJob>(
                    silenceExpireJobId,
                    job => job.Execute(server.Id),
                    "*/10 * * * *", // Every 10 minutes
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(silenceExpireJobId);

                // DiscordRoleExpireJob - every 10 minutes
                var discordRoleExpireJobId = $"DiscordRoleExpireJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<DiscordRoleExpireJob>(
                    discordRoleExpireJobId,
                    job => job.Execute(server.Id),
                    "*/10 * * * *", // Every 10 minutes
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(discordRoleExpireJobId);

                // UpdateServerDataJob - every 1 hours
                var updateServerDataJobId = $"UpdateServerDataJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<UpdateServerDataJob>(
                    updateServerDataJobId,
                    job => job.Execute(server.Id),
                    "0 */1 * * *", // Every 1 hours
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                jobIds.Add(updateServerDataJobId);

                // RaidTimesJob - every hour (start now)
                var raidTimesJobId = $"RaidTimesJob-{server.Id}";
                _recurringJobManager.AddOrUpdate<RaidTimesJob>(
                    raidTimesJobId,
                    job => job.Execute(server.Id),
                    "0 * * * *", // Every hour
                    new RecurringJobOptions
                    {
                        TimeZone = server.GetTimeZoneOrDefault()
                    });
                // Trigger immediately
                _backgroundJobClient.Enqueue<RaidTimesJob>(job => job.Execute(server.Id));
                jobIds.Add(raidTimesJobId);

                _ftpJobIds[server.Id] = jobIds;

                _logger.LogInformation("Successfully loaded {JobCount} FTP tasks for server id {ServerId}",
                    jobIds.Count, server.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule FTP tasks for server id {ServerId}", server.Id);
                throw;
            }
        }

        // Get job statistics
        public Dictionary<string, object> GetJobStatistics()
        {
            try
            {
                var api = JobStorage.Current.GetMonitoringApi();
                var statistics = api.GetStatistics();

                return new Dictionary<string, object>
                {
                    ["EnqueuedCount"] = statistics.Enqueued,
                    ["FailedCount"] = statistics.Failed,
                    ["ProcessingCount"] = statistics.Processing,
                    ["ScheduledCount"] = statistics.Scheduled,
                    ["SucceededCount"] = statistics.Succeeded,
                    ["DeletedCount"] = statistics.Deleted,
                    ["RecurringJobCount"] = statistics.Recurring,
                    ["Servers"] = statistics.Servers,
                    ["Queues"] = statistics.Queues
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job statistics");
                return new Dictionary<string, object>();
            }
        }

        // Health check method
        public bool IsSchedulerHealthy()
        {
            try
            {
                var api = JobStorage.Current.GetMonitoringApi();
                var statistics = api.GetStatistics();
                return statistics.Servers > 0; // At least one server is running
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler health check failed");
                return false;
            }
        }

        public void NewServerAddedAsync(ScumServer server)
        {
            ScheduleServerTasks(server);
            _cacheService.AddServers([server]);
        }

        public void FtpConfigAddedAsync(ScumServer server)
        {
            try
            {
                CleanupServerJobs(server.Id, "FtpJobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up FTP jobs for server {ServerId}", server.Id);
            }

            ScheduleFtpServerTasks(server);
            _cacheService.AddServers([server]);
        }

        public void DeleteJob(string jobKey, string groupKey)
        {
            var jobId = $"{jobKey}-{groupKey.Replace("ServerJobs(", "").Replace("FtpJobs(", "").Replace(")", "")}";
            _recurringJobManager.RemoveIfExists(jobId);
        }

        public async Task LoadAllServersTasks(CancellationToken cancellationToken)
        {
            var servers = await _scumServerRepository.FindActive();
            _cacheService.AddServers(servers);
            foreach (var server in await _scumServerRepository.GetActiveServersWithFtp())
            {
                ScheduleServerTasks(server, cancellationToken);
            }
        }

        public async Task LoadFtpAllServersTasks(CancellationToken cancellationToken)
        {
            foreach (var server in await _scumServerRepository.GetActiveServersWithFtp())
            {
                ScheduleFtpServerTasks(server, cancellationToken);
            }
        }

        public async Task LoadCustomServersTasks(CancellationToken cancellationToken)
        {
            foreach (var task in await _customTaskRepository.GetServersEnabledCustomTasks())
            {
                ScheduleCustomTask(task, cancellationToken);
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

        public void CreateWarzoneJobs(ScumServer server, Warzone warzone)
        {
            // CloseWarzoneJob - scheduled for specific time
            var closeWarzoneJobId = $"CloseWarzoneJob-{server.Id}-{warzone.Id}";
            _backgroundJobClient.Schedule<CloseWarzoneJob>(
                job => job.Execute(server.Id, warzone.Id),
                warzone.StopAt!.Value);

            // WarzoneItemSpawnJob - recurring
            var warzoneItemSpawnJobId = $"WarzoneItemSpawnJob-{server.Id}-{warzone.Id}";
            _recurringJobManager.AddOrUpdate<CloseWarzoneJob>(
                warzoneItemSpawnJobId,
                job => job.Execute(server.Id, warzone.Id),
                $"*/{warzone.ItemSpawnInterval} * * * *", // Every X minutes
                 new RecurringJobOptions
                 {
                     TimeZone = server.GetTimeZoneOrDefault()
                 });

            // Track these jobs for cleanup
            if (!_serverJobIds.ContainsKey(server.Id))
                _serverJobIds[server.Id] = new List<string>();

            _serverJobIds[server.Id].AddRange([closeWarzoneJobId, warzoneItemSpawnJobId]);
        }

        public void TriggerJob(string jobId, string groupId)
        {
            // Extract server ID from group and create Hangfire job ID
            var serverId = groupId.Replace("ServerJobs(", "").Replace("FtpJobs(", "").Replace(")", "");
            var hangfireJobId = $"{jobId}-{serverId}";

            _backgroundJobClient.Enqueue(() => TriggerJobByName(hangfireJobId));
        }

        [AutomaticRetry(Attempts = 1)]
        public void TriggerJobByName(string jobName)
        {
            // This method will be used to trigger specific jobs by name
            // You can implement logic to map job names to their execution methods
            _logger.LogInformation("Manually triggered job: {JobName}", jobName);
        }

        public List<JobModel> ListJobs()
        {
            var jobs = new List<JobModel>();

            try
            {
                var api = JobStorage.Current.GetMonitoringApi();
                List<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();

                foreach (var job in recurringJobs)
                {
                    jobs.Add(new JobModel
                    {
                        JobID = job.Id,
                        GroupID = "RecurringJobs", // Hangfire doesn't use groups like Quartz
                        NextFireTime = job.NextExecution
                    });
                }

                // Add scheduled jobs
                var scheduledJobs = api.ScheduledJobs(0, int.MaxValue);
                foreach (var job in scheduledJobs)
                {
                    jobs.Add(new JobModel
                    {
                        JobID = job.Key,
                        GroupID = "ScheduledJobs",
                        NextFireTime = job.Value.EnqueueAt
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list jobs");
            }

            return jobs;
        }

        public RecurringJobDto? FindJob(string jobName)
        {
            var jobs = new List<JobModel>();
            var api = JobStorage.Current.GetMonitoringApi();
            List<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            return recurringJobs.FirstOrDefault(x => x.Id.StartsWith(jobName));
        }

        public void DeleteWarzoneJobs(ScumServer server)
        {
            try
            {
                var api = JobStorage.Current.GetMonitoringApi();
                List<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();

                // Find and remove warzone-related jobs for this server
                var warzoneJobs = recurringJobs.Where(job =>
                    job.Id.StartsWith($"CloseWarzoneJob-{server.Id}") ||
                    job.Id.StartsWith($"WarzoneItemSpawnJob-{server.Id}"));

                foreach (var job in warzoneJobs)
                {
                    _recurringJobManager.RemoveIfExists(job.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete warzone jobs for server {ServerId}", server.Id);
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

            ScheduleCustomTask(customTask);

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

            ScheduleCustomTask(customTask);

            return _mapper.Map<CustomTaskDto>(customTask);
        }

        public async Task<CustomTaskDto> DeleteCustomTask(long id)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) throw new NotFoundException("Invalid server");
            ValidateSubscription(server);

            var customTask = await _customTaskRepository.FindByIdAsync(id);
            if (customTask == null) throw new NotFoundException("CustomTask not found");

            var jobId = $"CustomTaskJob-{customTask.ScumServerId}-{customTask.Id}";

            _customTaskRepository.Delete(customTask);
            await _customTaskRepository.SaveAsync();

            try
            {
                _recurringJobManager.RemoveIfExists(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove custom task job {JobId}", jobId);
            }

            return _mapper.Map<CustomTaskDto>(customTask);
        }

        public void ScheduleCustomTask(CustomTask customTask, CancellationToken cancellationToken = default)
        {
            var jobId = $"CustomTaskJob-{customTask.ScumServerId}-{customTask.Id}";

            try
            {
                _recurringJobManager.RemoveIfExists(jobId);
            }
            catch (Exception) { }

            _recurringJobManager.AddOrUpdate<CustomTaskJob>(
                jobId,
                job => job.Execute(customTask.ScumServerId!.Value, customTask.Id),
                customTask.Cron,
                new RecurringJobOptions
                {
                    TimeZone = customTask.ScumServer.GetTimeZoneOrDefault()
                });
        }
    }
}