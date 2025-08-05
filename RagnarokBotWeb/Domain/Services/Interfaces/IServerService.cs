using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IServerService
    {
        Task<ScumServerDto> ChangeFtp(FtpDto ftpDto);
        Task AddGuild(ChangeGuildDto guildDto);
        Task<ScumServer> GetServer();
        Task<ScumServer> GetServer(long serverId);
        Task<GuildDto> ConfirmDiscordToken(SaveDiscordSettingsDto settings);
        Task<GuildDto> RunDiscordTemplate();
        Task<GuildDto> GetServerDiscord();
        Task<ScumServerDto> SaveServerDiscordChannels(List<SaveChannelDto> channels);
        Task<ScumServerDto?> SaveServerDiscordChannel(SaveChannelDto saveChannel);
        Task<List<SaveChannelDto>> GetServerDiscordChannels();
        Task<List<DiscordRolesDto>> GetServerDiscordRoles();
        Task<ScumServerDto> UpdateServerSettings(UpdateServerSettingsDto updateServer);
        Task<ScumServerDto> UpdateKillFeed(UpdateKillFeedDto updateKillFeed);
        Task UpdateServerData(ScumServer server);
        Task<PlayerCountDto> GetServerPlayerCount();
    }
}
