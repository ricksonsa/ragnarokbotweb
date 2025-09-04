using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Handlers.ChangeFileHandler
{
    public class ChangeFileHandlerFactory
    {
        private readonly IFtpService _ftpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Dictionary<EFileChangeType, string> _files = new()
        {
            { EFileChangeType.BannedUsers, "BannedUsers.ini" },
            { EFileChangeType.Whitelist, "WhitelistedUsers.ini" },
            { EFileChangeType.SilencedUsers, "SilencedUsers.ini" },
            { EFileChangeType.ServerSettings, "ServerSettings.ini" },
        };

        public ChangeFileHandlerFactory(IFtpService ftpService, IUnitOfWork unitOfWork)
        {
            _ftpService = ftpService;
            _unitOfWork = unitOfWork;
        }

        public IChangeFileHandler CreateFileLineHandler(EFileChangeType changeType)
        {
            return new FileLineChangeHandler(_files[changeType], _ftpService, _unitOfWork);
        }
    }
}
