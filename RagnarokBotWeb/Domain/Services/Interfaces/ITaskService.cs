using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ITaskService
    {
        Task LoadFtpAllServersTasks(CancellationToken cancellationToken);
        Task LoadAllServersTasks(CancellationToken cancellationToken);
        Task NewServerAddedAsync(ScumServer server);
        Task FtpConfigAddedAsync(ScumServer server);
        Task DeleteJob(string jobKey);
        Task LoadRaidTimes(CancellationToken stoppingToken);
        Task LoadSquads(CancellationToken stoppingToken);
        Task LoadFlags(CancellationToken stoppingToken);
        Task CreateWarzoneJobs(ScumServer server, Warzone warzone);
        Task DeleteWarzoneJobs(ScumServer server);
    }
}
