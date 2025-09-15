using Hangfire.Storage;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ITaskService
    {
        Task LoadFtpAllServersTasks(CancellationToken cancellationToken);
        Task LoadCustomServersTasks(CancellationToken cancellationToken);
        Task LoadAllServersTasks(CancellationToken cancellationToken);
        void NewServerAddedAsync(ScumServer server);
        void FtpConfigAddedAsync(ScumServer server);
        void DeleteJob(string jobKey, string groupKey);
        Task LoadRaidTimes(CancellationToken stoppingToken);
        Task LoadSquads(CancellationToken stoppingToken);
        Task LoadFlags(CancellationToken stoppingToken);
        void CreateWarzoneJobs(ScumServer server, Warzone warzone);
        void DeleteWarzoneJobs(ScumServer server);
        void TriggerJob(string jobId, string groupId);
        List<JobModel> ListJobs();
        Task<Page<CustomTaskDto>> GetTaskPageByFilterAsync(Paginator paginator, string? filter);
        Task<CustomTaskDto?> FetchTaskById(long id);
        Task<CustomTaskDto> CreateTask(CustomTaskDto task);
        Task<CustomTaskDto> UpdateTask(long id, CustomTaskDto task);
        void ScheduleCustomTask(CustomTask customTask, CancellationToken cancellationToken = default);
        Task<CustomTaskDto> DeleteCustomTask(long id);
        bool IsSchedulerHealthy();
        Dictionary<string, object> GetJobStatistics();
        Task<CustomTaskDto> DeleteCustomTaskFromJob(long id);
        Task<CustomTaskDto> UpdateTaskFromJob(long id, CustomTaskDto customTaskDto);
        RecurringJobDto? FindJob(string jobName);
        Task<IEnumerable<IdsDto>> GetAllTaskIds();
    }
}
