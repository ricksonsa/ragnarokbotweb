using FluentFTP;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IFtpService
    {
        FtpClient GetClient(Ftp ftp);
        void CopyFiles(FtpClient client, string targetFolder, IList<string> remoteFilePaths);
    }
}
