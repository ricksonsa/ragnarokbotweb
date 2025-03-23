using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class ChannelService(IChannelRepository channelRepository) : IChannelService
{
    public Task<Channel?> FindByGuildIdAndChannelTypeAsync(long guildId, ChannelTemplateValue channelType)
    {
        return channelRepository
            .FindOneAsync(channel => channel.ChannelType == channelType.ToString() && channel.Guild.Id == guildId);
    }

    public async Task CreateChannelAsync(Channel channel)
    {
        await channelRepository.AddAsync(channel);
        await channelRepository.SaveAsync();
    }

    public async Task DeleteAllAsync()
    {
        foreach (var channel in await channelRepository.GetAllAsync()) channelRepository.Delete(channel);
        await channelRepository.SaveAsync();
    }
}