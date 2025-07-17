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

        public Warzone? Select(List<Warzone> warzones)
        {
            var running = warzones.FirstOrDefault(warzone => warzone.IsRunning);
            if (running is not null) return running;

            foreach (var warzone in warzones.OrderBy(warzone => warzone.LastRunned))
            {
                var onlinePlayerCount = _cacheService.GetConnectedPlayers(_scumServer.Id).Count();

                if (warzone.MinPlayerOnline < onlinePlayerCount) continue;

                return warzone;
            }

            return null;
        }

    }
}
