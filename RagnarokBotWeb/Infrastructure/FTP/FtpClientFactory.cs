using FluentFTP;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.FTP
{
    public class FtpClientFactory
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public FtpClientFactory(string host, int port, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
            _port = port;
        }

        public FtpClient CreateClient()
        {
            var client = new FtpClient(_host, port: _port, user: _username, pass: _password);
            var ftpConfig = new FtpConfig();
            ftpConfig.LogHost = true;
            ftpConfig.LogToConsole = true;
            ftpConfig.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            ftpConfig.ConnectTimeout = 50000;
            ftpConfig.DataConnectionType = FtpDataConnectionType.AutoPassive;
            // ftpConfig.LogToConsole = false;
            client.Config = ftpConfig;
            client.AutoConnect();
            return client;
        }

        public FtpClient CreateClient(Ftp ftp)
        {
            var client = new FtpClient(ftp.Address, port: (int)ftp.Port, user: ftp.UserName, pass: ftp.Password);
            var ftpConfig = new FtpConfig();
            ftpConfig.LogHost = true;
            ftpConfig.LogToConsole = true;
            ftpConfig.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            ftpConfig.ConnectTimeout = 50000;
            ftpConfig.DataConnectionType = FtpDataConnectionType.AutoPassive;
            client.Config = ftpConfig;
            client.AutoConnect();
            return client;
        }
    }
}
