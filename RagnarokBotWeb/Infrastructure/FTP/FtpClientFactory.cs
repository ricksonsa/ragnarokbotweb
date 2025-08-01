using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Security.Authentication;

namespace RagnarokBotWeb.Infrastructure.FTP;
public static class FtpClientFactory
{
    public static FtpClient CreateClient(Ftp ftp)
    {
        if (string.IsNullOrWhiteSpace(ftp.Address) || string.IsNullOrWhiteSpace(ftp.UserName))
            throw new ArgumentException("FTP configuration is invalid.");

        var client = new FtpClient(ftp.Address, port: (int)ftp.Port, user: ftp.UserName, pass: ftp.Password)
        {
            Config = new FtpConfig
            {
                LogHost = false,
                LogDurations = false,
                LogPassword = false,
                LogUserName = false,
                LogToConsole = false,
                SslProtocols = SslProtocols.None,
                EncryptionMode = FtpEncryptionMode.Auto,
                ConnectTimeout = 5000,
                DataConnectionType = FtpDataConnectionType.AutoPassive

            }
        };

        return client;
    }
}
