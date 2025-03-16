using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IServerService
    {
        Task<ScumServerDto> ChangeFtp(FtpDto ftpDto);
        Task AddGuild(ChangeGuildDto guildDto);
        Task<ScumServer> GetServer(long serverId);
        Task<GuildDto> ConfirmDiscordToken(SaveDiscordSettingsDto settings);
    }
}
