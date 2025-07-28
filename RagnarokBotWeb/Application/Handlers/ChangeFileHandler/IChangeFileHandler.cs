using RagnarokBotWeb.Application.Models;

namespace RagnarokBotWeb.Application.Handlers.ChangeFileHandler
{
    public interface IChangeFileHandler
    {
        public Task Handle(FileChangeCommand command);
    }
}
