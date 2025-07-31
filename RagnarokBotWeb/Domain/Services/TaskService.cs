using Quartz;
using Quartz.Impl.Matchers;
using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class TaskService : ITaskService
    {
        private readonly ILogger<TaskService> _logger;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ICacheService _cacheService;

        public TaskService(
            ISchedulerFactory schedulerFactory,
            IScumServerRepository scumServerRepository,
            ICacheService cacheService,
            ILogger<TaskService> logger)
        {
            _schedulerFactory = schedulerFactory;
            _scumServerRepository = scumServerRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        private static ITrigger CronTrigger(string cron) => TriggerBuilder.Create()
                        .WithCronSchedule(cron)
                        .Build();

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

        private static ITrigger EveryDayTrigger() => TriggerBuilder.Create()
                          .WithCronSchedule(AppSettingsStatic.EveryDayCron)
                          .Build();

        private async Task ScheduleServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            var job = JobBuilder.Create<BotAliveJob>()
                   .WithIdentity($"BotAliveJob({server.Id})", $"ServerJobs({server.Id})")
                   .UsingJobData("server_id", server.Id)
                   .Build();
            await scheduler.ScheduleJob(job, DefaultTrigger());

            job = JobBuilder.Create<ListPlayersJob>()
                .WithIdentity($"ListPlayersJob({server.Id})", $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<OrderCommandJob>()
                 .WithIdentity($"OrderCommandJob({server.Id})", $"ServerJobs({server.Id})")
                 .UsingJobData("server_id", server.Id)
                 .Build();
            await scheduler.ScheduleJob(job, TenSecondsTrigger());

            job = JobBuilder.Create<WarzoneBootstartJob>()
                .WithIdentity($"WarzoneBootstartJob({server.Id})", $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<CoinAwardJob>()
                .WithIdentity($"{nameof(CoinAwardJob)}({server.Id})", $"ServerJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0 0/30 * * * ?"));


            _logger.LogInformation("Loaded server tasks for server id {Id}", server.Id);
        }

        private async Task ScheduleFtpServerTasks(ScumServer server, CancellationToken cancellationToken = default)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            var job = JobBuilder.Create<ChatJob>()
                .WithIdentity($"ChatJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Chat.ToString())
                .Build();
            await scheduler.ScheduleJob(job, CronTrigger("0/30 * * * * ?"));

            job = JobBuilder.Create<KillLogJob>()
                   .WithIdentity($"KillLogJob({server.Id})", $"FtpJobs({server.Id})")
                   .UsingJobData("server_id", server.Id)
                   .UsingJobData("file_type", EFileType.Kill.ToString())
                   .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<EconomyJob>()
                .WithIdentity($"EconomyJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Economy.ToString())
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<GamePlayJob>()
                .WithIdentity($"GamePlayJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Gameplay.ToString())
                .Build();
            await scheduler.ScheduleJob(job, FiveMinTrigger());

            job = JobBuilder.Create<LoginJob>()
                .WithIdentity($"LoginJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .UsingJobData("file_type", EFileType.Login.ToString())
                .Build();
            await scheduler.ScheduleJob(job, DefaultTrigger());

            job = JobBuilder.Create<VipExpireJob>()
                .WithIdentity($"VipExpireJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TwoMinTrigger());

            job = JobBuilder.Create<BanExpireJob>()
                .WithIdentity($"BanExpireJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TwoMinTrigger());

            job = JobBuilder.Create<SilenceExpireJob>()
                .WithIdentity($"SilenceExpireJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TwoMinTrigger());

            job = JobBuilder.Create<DiscordRoleExpireJob>()
                .WithIdentity($"DiscordRoleExpireJob({server.Id})", $"FtpJobs({server.Id})")
                .UsingJobData("server_id", server.Id)
                .Build();
            await scheduler.ScheduleJob(job, TwoMinTrigger());

            job = JobBuilder.Create<FileChangeJob>()
                .WithIdentity($"FileChangeJob({server.Id})", $"FtpJobs({server.Id})")
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

                if (jobKeys.Any())
                {
                    await scheduler.DeleteJobs(jobKeys.ToList());
                }
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
    }
}
