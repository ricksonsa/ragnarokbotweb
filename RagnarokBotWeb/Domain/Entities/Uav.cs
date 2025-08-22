
using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Uav : BaseOrderEntity
    {
        public bool SendToUserDM { get; set; }

        public Uav()
        {
            Name = "🛰️ UAV Scan Report";
            Description = "Real-time reconnaissance data from an unmanned aerial vehicle. Use this intelligence to track movement, locate targets, or prepare for engagement.";
            DeliveryText = "UAV is now scanning sector {sector}";
        }
    }
}
