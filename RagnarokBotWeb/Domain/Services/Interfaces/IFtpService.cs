using FluentFTP;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IFtpService
    {
        Task<AsyncFtpClient> GetClientAsync(Ftp ftp, CancellationToken cancellationToken = default);
        Task CopyFilesAsync(AsyncFtpClient client, string targetFolder, IList<string> remoteFilePaths, CancellationToken token = default);
        Task RemoveLine(AsyncFtpClient client, string remotePath, string lineToRemove);
        Task AddLine(AsyncFtpClient client, string remotePath, string lineToAdd);
        Task UpdateINILine(AsyncFtpClient client, string remoteFilePath, string key, string newValue);
        Task<Stream?> DownloadFile(AsyncFtpClient client, string remoteFilePath);
        Task ReleaseClientAsync(AsyncFtpClient client);
        Task<T> ExecuteWithRetryAsync<T>(Ftp ftp, Func<AsyncFtpClient, Task<T>> operation, CancellationToken cancellationToken = default);
        Task ClearPoolForServerAsync(Ftp ftp);
        Task GetServerConfigLineValue(AsyncFtpClient client, string remoteFilePath, Dictionary<string, string> data);
    }
}
