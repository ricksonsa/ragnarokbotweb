using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class ChannelService(IChannelRepository channelRepository) : IChannelService
{
    public Task<Channel> FindByGuildIdAndChannelTypeAsync(long guildId, EChannelType channelType)
    {
        return channelRepository.FindOneAsync(x => x.ChannelType == channelType && x.Guild.Id == guildId);
    }

    public async Task CreateChannelAsync(Channel channel)
    {
        await channelRepository.AddAsync(channel);
        await channelRepository.SaveAsync();
    }
}