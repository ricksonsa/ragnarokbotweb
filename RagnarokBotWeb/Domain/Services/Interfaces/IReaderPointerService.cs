using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Interfaces;

public interface IReaderPointerService
{
    Task<ReaderPointer?> GetReaderPointer(DateTime datetime, long scumServerId, EFileType fileType);
}