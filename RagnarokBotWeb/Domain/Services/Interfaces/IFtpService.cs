using FluentFTP;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IFtpService
    {
        FtpClient GetClient();
    }
}
