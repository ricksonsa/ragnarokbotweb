using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IDiscordService
    {
        Task<Guild> CreateChannelTemplates(long serverId);
        Task SendEmbed(CreateEmbed createEmbed);
    }
}
