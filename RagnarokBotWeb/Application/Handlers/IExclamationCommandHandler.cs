using RagnarokBotWeb.Application.Models;

namespace RagnarokBotWeb.Application.Handlers
{
    public interface IExclamationCommandHandler
    {
        public Task ExecuteAsync(ChatTextParseResult value);
    }
}
