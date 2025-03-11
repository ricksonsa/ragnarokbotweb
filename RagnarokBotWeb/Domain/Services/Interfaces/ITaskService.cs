using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ITaskService
    {
        Task LoadFtpServerTasks(CancellationToken cancellationToken);
        Task LoadServerTasks(CancellationToken cancellationToken);
        Task NewServerAddedAsync(ScumServer server);
        Task FtpConfigAddedAsync(ScumServer server);
    }
}
