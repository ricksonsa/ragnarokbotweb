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
        Task<ScumServerDto?> SaveServerDiscordChannel(SaveChannelDto saveChannel);
        Task<List<SaveChannelDto>> GetServerDiscordChannels();
        Task<List<DiscordRolesDto>> GetServerDiscordRoles();
        Task<ScumServerDto> UpdateServerSettings(UpdateServerSettingsDto updateServer);
        Task<ScumServerDto> UpdateKillFeed(UpdateKillFeedDto updateKillFeed);
        Task UpdateServerData(ScumServer server);
        Task<List<PlayerDto>> GetOnlinePlayers();
        Task<List<Shared.Models.ScumPlayer>> GetOnlineScumPlayers();
        Task<UavDto> UpdateUav(UavDto dto);
        Task<ExchangeDto> UpdateExchange(ExchangeDto dto);
    }
}
