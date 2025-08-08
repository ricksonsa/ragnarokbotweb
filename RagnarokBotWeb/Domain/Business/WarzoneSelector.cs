using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Domain.Business
{
    public class WarzoneSelector
    {
        private readonly ICacheService _cacheService;
        private readonly ScumServer _scumServer;

        public WarzoneSelector(
            ICacheService cacheService,
            ScumServer scumServer)
        {
            _cacheService = cacheService;
            _scumServer = scumServer;
        }

        public Warzone? Select(List<Warzone> warzones, bool? force = false)
        {
            var raidTime = _cacheService.GetRaidTimes(_scumServer.Id);
            var running = warzones.FirstOrDefault(warzone => warzone.IsRunning);
            if (running is not null && force.HasValue && !force.Value) return running;

            foreach (var warzone in warzones.OrderBy(warzone => warzone.LastRunned))
            {
                if (force.HasValue && !force.Value && !warzone.Enabled) continue;
                var onlinePlayerCount = _cacheService.GetConnectedPlayers(_scumServer.Id).Count();

                if (warzone.MinPlayerOnline < onlinePlayerCount && force.HasValue && !force.Value) continue;
                if (warzone.IsBlockPurchaseRaidTime && raidTime != null && raidTime.IsInRaidTime(_scumServer) && force.HasValue && !force.Value) continue;

                return warzone;
            }

            return null;
        }

    }
}
