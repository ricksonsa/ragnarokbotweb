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
}