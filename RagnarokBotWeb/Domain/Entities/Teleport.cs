using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Teleport : BaseEntity
    {
        public required string Name { get; set; }
        public required string Coordinates { get; set; }
    }
}
