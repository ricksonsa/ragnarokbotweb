using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Handlers.ChangeFileHandler
{
    public class AddRemoveLineHandler : IChangeFileHandler
    {
        private readonly IFtpService _ftpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _file;
        public AddRemoveLineHandler(string file, IFtpService ftpService, IUnitOfWork unitOfWork)
        {
            _ftpService = ftpService;
            _unitOfWork = unitOfWork;
            _file = file;
        }
        public async Task Handle(FileChangeCommand command)
        {
            var server = await _unitOfWork.ScumServers
                .Include(server => server.Ftp)
                .FirstOrDefaultAsync(server => server.Id == command.ServerId);

            if (server!.Ftp is null) throw new Exception("Server does not have a ftp configuration");
            var client = await _ftpService.GetClientAsync(server.Ftp);
            var remotePath = server!.Ftp.RootFolder + "/Saved/Config/WindowsServer/" + _file;

            switch (command.FileChangeMethod)
            {
                case Domain.Enums.EFileChangeMethod.AddLine:
                    await _ftpService.AddLine(client, remotePath, command.Value);
                    break;

                case Domain.Enums.EFileChangeMethod.RemoveLine:
                    await _ftpService.RemoveLine(client, remotePath, command.Value);
                    break;
            }

            await _ftpService.ReleaseClientAsync(client);
        }
    }
}
