using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Taxi : BaseOrderEntity
    {
        public ETaxiType TaxiType { get; set; }
        public List<TaxiTeleport> TaxiTeleports { get; set; }

    }
}
