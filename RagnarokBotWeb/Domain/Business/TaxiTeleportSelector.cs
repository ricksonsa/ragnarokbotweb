using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Business
{
    public class TaxiTeleportSelector
    {
        private static readonly Random _random = new();

        public static TaxiTeleport SelectTeleportPoint(Taxi taxi)
        {
            int r = _random.Next(taxi.TaxiTeleports.Count);
            return taxi.TaxiTeleports[r];

        }
    }
}
