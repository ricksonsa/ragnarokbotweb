using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.FTP;

namespace RagnarokBotWeb.Domain.Services;

public class FtpService(FtpConnectionPool pool) : IFtpService
{
    public FtpClient GetClient(Ftp ftp)
    {
        return pool.GetClient(ftp);
    }

    public void CopyFiles(FtpClient client, string targetFolder, IList<string> remoteFilePaths)
    {
        var states = client.DownloadFiles(targetFolder, remoteFilePaths, FtpLocalExists.Overwrite, FtpVerify.Throw);
        if (states.Any(result => result.IsFailed)) throw new Exception("Error while copying files from FTP");
    }
}