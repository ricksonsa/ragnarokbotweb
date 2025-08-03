using Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using static RagnarokBotWeb.Application.Tasks.Jobs.KillRankJob;
using static RagnarokBotWeb.Application.Tasks.Jobs.LockpickRankJob;

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
        Task<IGuildUser?> GetDiscordUser(ulong guildId, ulong userId);
        Task SendKillFeedEmbed(ScumServer server, Kill kilL);
        Task DeleteAllMessagesInChannel(ulong channelId);
        Task SendTopPlayersKillsEmbed(ulong channelId, List<PlayerStatsDto> players, ERankingPeriod period, int topCount);
        Task SendTopDistanceKillsEmbed(ulong channelId, List<PlayerStatsDto> players, int topCount);
        Task SendLockpickRankEmbed(ulong channelId, List<LockpickStatsDto> stats, string lockType);
    }
}
