using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces;

public interface IChannelTemplateService
{
    Task<IEnumerable<ChannelTemplate>> GetAllAsync();
}