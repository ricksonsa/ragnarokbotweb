using System.Security.Authentication;
using FluentFTP;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.FTP;

public static class FtpClientFactory
{
    public static FtpClient CreateClient(Ftp ftp)
    {
        var client = new FtpClient(ftp.Address, port: (int)ftp.Port, user: ftp.UserName, pass: ftp.Password);
        var ftpConfig = new FtpConfig
        {
            LogHost = true,
            LogToConsole = true,
            SslProtocols = SslProtocols.Tls12,
            ConnectTimeout = 50000,
            DataConnectionType = FtpDataConnectionType.AutoPassive
        };
        client.Config = ftpConfig;
        client.AutoConnect();
        return client;
    }
}