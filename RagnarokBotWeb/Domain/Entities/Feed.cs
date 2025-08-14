using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Feed : BaseEntity
    {
        public EFeedType FeedType { get; set; } = EFeedType.None;
        public required string Text { get; set; }
        public required ScumServer Server { get; set; }
    }
}
