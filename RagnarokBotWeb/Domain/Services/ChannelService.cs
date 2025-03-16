using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class ChannelService(IChannelRepository channelRepository) : IChannelService
{
    public Task<Channel?> FindByGuildIdAndChannelTypeAsync(long guildId, EChannelType channelType)
    {
        return channelRepository
            .FindOneAsync(channel => channel.ChannelType == channelType && channel.Guild.Id == guildId);
    }

    public async Task CreateChannelAsync(Channel channel)
    {
        await channelRepository.AddAsync(channel);
        await channelRepository.SaveAsync();
    }

    public Task DeleteAllAsync()
    {
        foreach (var channel in channelRepository.GetAllAsync().Result) channelRepository.Delete(channel);
        return channelRepository.SaveAsync();
    }
}