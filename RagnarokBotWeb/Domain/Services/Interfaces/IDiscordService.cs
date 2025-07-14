using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IDiscordService
    {
        Task<Guild> CreateChannelTemplates(long serverId);
        Task SendEmbedToChannel(CreateEmbed createEmbed);
        Task SendEmbedToUserDM(CreateEmbed createEmbed);
        string GetDiscordUserName(ulong discordId);
    }
}
