using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class ChannelTemplateService(IChannelTemplateRepository channelTemplateRepository) : IChannelTemplateService
{
    public Task<IEnumerable<ChannelTemplate>> GetAllAsync()
    {
        return channelTemplateRepository.GetAllAsync();
    }
}