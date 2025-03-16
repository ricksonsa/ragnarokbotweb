using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Interfaces;

public interface IChannelService
{
    Task<Channel?> FindByGuildIdAndChannelTypeAsync(long guildId, EChannelType channelType);
    
    Task CreateChannelAsync(Channel channel);
    
    // TODO: remove when end tests
    Task DeleteAllAsync();
}