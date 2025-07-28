using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

public interface IChannelRepository : IRepository<Channel>
{
    Task<Channel?> FindOneByServerIdAndChatType(long serverId, string chatType);
    Task<List<Channel>> FindAllByServerId(long serverId);
}