using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class ChannelTemplate : BaseEntity
    {
        public string Name { get; set; }
        public string? CategoryName { get; set; }
        public EChannelType ChannelType { get; set; }
    }
}
