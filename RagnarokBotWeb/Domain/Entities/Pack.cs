
using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Pack : BaseOrderEntity
    {
        public List<PackItem> PackItems { get; set; }
        public bool IsWelcomePack { get; set; }
    }
}
