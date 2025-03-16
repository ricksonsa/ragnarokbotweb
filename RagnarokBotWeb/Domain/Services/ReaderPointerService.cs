using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class ReaderPointerService : IReaderPointerService
{
    private readonly IReaderPointerRepository _repository;

    public ReaderPointerService(IReaderPointerRepository repository)
    {
        _repository = repository;
    }

    public Task<ReaderPointer?> GetReaderPointer(DateTime datetime, long scumServerId, EFileType fileType)
    {
        return _repository
            .FindOneAsync(pointer =>
                pointer.ScumServer.Id == scumServerId
                && pointer.FileType == fileType
                && pointer.FileDate.Date == datetime.Date
            );
    }
}