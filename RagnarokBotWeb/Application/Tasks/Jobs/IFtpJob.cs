using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public interface IFtpJob
    {
        public Task Execute(long serverId, EFileType fileType);
    }
}
