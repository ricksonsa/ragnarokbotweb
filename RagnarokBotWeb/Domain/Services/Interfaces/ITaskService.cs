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
        Task NewServerAddedAsync(ScumServer server);
        Task FtpConfigAddedAsync(ScumServer server);
        Task DeleteJob(string jobKey, string groupKey);
        Task LoadRaidTimes(CancellationToken stoppingToken);
        Task LoadSquads(CancellationToken stoppingToken);
        Task LoadFlags(CancellationToken stoppingToken);
        Task CreateWarzoneJobs(ScumServer server, Warzone warzone);
        Task DeleteWarzoneJobs(ScumServer server);
        Task TriggerJob(string jobId, string groupId);
        Task<List<JobModel>> ListJobs();
        Task<Page<CustomTaskDto>> GetTaskPageByFilterAsync(Paginator paginator, string? filter);
        Task<CustomTaskDto?> FetchTaskById(long id);
        Task<CustomTaskDto> CreateTask(CustomTaskDto task);
        Task<CustomTaskDto> UpdateTask(long id, CustomTaskDto task);
        Task ScheduleCustomTask(CustomTask customTask, CancellationToken cancellationToken = default);
        Task<CustomTaskDto> DeleteCustomTask(long id);
    }
}
