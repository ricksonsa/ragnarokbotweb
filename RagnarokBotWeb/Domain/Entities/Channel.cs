using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Channel : BaseEntity
    {
        public Guild Guild { get; set; }
        public EChannelType ChannelType { get; set; }
        public ulong DiscordId { get; set; }
    }
}
