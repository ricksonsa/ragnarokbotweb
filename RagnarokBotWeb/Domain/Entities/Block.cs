using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Block : BaseEntity
    {
        public EEntityType EntityType { get; set; }
        public DateTime BlockDate { get; set; }
        public bool Active { get; set; }
        public string Value { get; set; }
    }
}
