using Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IDiscordService
    {
        Task<Guild> CreateChannelTemplates(long serverId);
        Task<IUserMessage> SendEmbedToChannel(CreateEmbed createEmbed);
        Task SendEmbedToUserDM(CreateEmbed createEmbed);
        string GetDiscordUserName(ulong discordId);
        Task RemoveMessage(ulong channelId, ulong messageId);
        Task<IUserMessage> SendEmbedWithBase64Image(CreateEmbed createEmbed);
        Task<IUserMessage?> CreateButtonAsync(ulong discordId, ButtonTemplate buttonTemplate);
        Task AddUserRoleAsync(ulong guildId, ulong userDiscordId, ulong roleId);
        Task RemoveUserRoleAsync(ulong guildId, ulong userDiscordId, ulong roleId);
    }
}
