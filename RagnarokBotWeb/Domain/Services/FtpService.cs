using FluentFTP;
using Microsoft.Extensions.Options;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.FTP;

namespace RagnarokBotWeb.Domain.Services
{
    public class FtpService : IFtpService
    {
        private readonly FtpClientFactory _ftpClientFactory;
        public FtpService(IOptions<AppSettings> options)
        {
            var appsettings = options.Value;
            _ftpClientFactory = new FtpClientFactory(appsettings.FtpHost, appsettings.FtpPort, appsettings.FtpUser, appsettings.FtpPassword);
        }

        public FtpClient GetClient()
        {
            return _ftpClientFactory.CreateClient();
        }

        public FtpClient GetClient(Ftp ftp)
        {
            return _ftpClientFactory.CreateClient(ftp);
        }

    }
}
