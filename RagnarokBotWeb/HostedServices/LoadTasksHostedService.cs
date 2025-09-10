//using Microsoft.EntityFrameworkCore;
//using RagnarokBotWeb.Application.Tasks.Jobs;
//using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

//namespace RagnarokBotWeb.HostedServices
//{
//    public class LoadTasksHostedService : BackgroundService
//    {
//        private readonly ILogger<LoadTasksHostedService> _logger;
//        private readonly IServiceProvider _services;

//        public LoadTasksHostedService(
//            ILogger<LoadTasksHostedService> logger,
//            IServiceProvider services)
//        {
//            _logger = logger;
//            _services = services;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.Log(LogLevel.Information, "Loading Scheduled Tasks");
//            using var scope = _services.CreateScope();
//            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

//            var tasks = await uow.ScheduledTasks
//                .Include(task => task.ScumServer)
//                .Where(task => task.IsActive)
//                .ToListAsync(cancellationToken: stoppingToken);

//            foreach (var task in tasks)
//            {
//                var job = JobBuilder.Create<CustomJob>()
//                   .WithIdentity(task.Name)
//                   .UsingJobData("server_id", task.ScumServer.Id)
//                   .UsingJobData("commands", string.Join(";", task.Commands))
//                   .Build();

//                var trigger = TriggerBuilder.Create()
//                    .WithCronSchedule(task.CronExpression)
//                    .Build();

//                await scheduler.ScheduleJob(job, trigger);
//            }
//        }
//    }
//}
