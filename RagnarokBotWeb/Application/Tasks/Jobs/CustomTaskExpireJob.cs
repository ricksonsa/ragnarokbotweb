using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CustomTaskExpireJob : AbstractJob, IJob
    {
        private readonly ILogger<CustomTaskExpireJob> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskService _taskService;

        public CustomTaskExpireJob(
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            ILogger<CustomTaskExpireJob> logger,
            ITaskService taskService) : base(scumServerRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _taskService = taskService;
        }

        public async Task Execute(long serverId)
        {
            try
            {
                var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);
                _logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

                var tasks = await _unitOfWork.CustomTasks
                    .Include(task => task.ScumServer)
                    .AsNoTracking()
                    .Where(task =>
                        task.ScumServer != null
                        && task.ScumServer.Id == server.Id
                        && task.ExpireAt.HasValue && task.ExpireAt.Value < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var task in tasks)
                {
                    if (task.DeleteExpired)
                    {
                        await _taskService.DeleteCustomTaskFromJob(task.Id);
                    }
                    else
                    {
                        await _taskService.UpdateTaskFromJob(task.Id, new Domain.Services.Dto.CustomTaskDto
                        {
                            Enabled = false,
                            Cron = task.Cron,
                            Commands = task.Commands,
                            Id = task.Id,
                            Description = task.Description,
                            LastRunned = task.LastRunned,
                            IsBlockPurchaseRaidTime = task.IsBlockPurchaseRaidTime,
                            Name = task.Name,
                            MinPlayerOnline = task.MinPlayerOnline,
                            ScumServerId = task.ScumServerId,
                            StartMessage = task.StartMessage,
                            ExpireAt = null,
                            TaskType = task.TaskType
                        });
                        _unitOfWork.CustomTasks.Update(task);
                        await _unitOfWork.SaveAsync();
                    }
                }
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
