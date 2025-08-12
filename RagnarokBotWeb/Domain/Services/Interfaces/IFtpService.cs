using FluentFTP;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IFtpService
    {
        AsyncFtpClient GetClient(Ftp ftp, CancellationToken cancellationToken = default);
        Task CopyFilesAsync(AsyncFtpClient client, string targetFolder, IList<string> remoteFilePaths, CancellationToken token = default);
        Task RemoveLine(AsyncFtpClient client, string remotePath, string lineToRemove);
        Task AddLine(AsyncFtpClient client, string remotePath, string lineToAdd);
        Task UpdateINILine(AsyncFtpClient client, string remoteFilePath, string key, string newValue);
        Task<Stream?> DownloadFile(AsyncFtpClient client, string remoteFilePath);
        void ReleaseClient(AsyncFtpClient client);
    }
}
