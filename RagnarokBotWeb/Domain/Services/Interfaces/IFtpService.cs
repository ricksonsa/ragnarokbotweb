using FluentFTP;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IFtpService
    {
        FtpClient GetClient(Ftp ftp);
        void CopyFiles(FtpClient client, string targetFolder, IList<string> remoteFilePaths);
        Task RemoveLine(FtpClient client, string remotePath, string lineToRemove);
        Task AddLine(FtpClient client, string remotePath, string lineToAdd);
        Task UpdateINILine(FtpClient client, string remoteFilePath, string key, string newValue);
        Stream? DownloadFile(FtpClient client, string remoteFilePath);
    }
}
