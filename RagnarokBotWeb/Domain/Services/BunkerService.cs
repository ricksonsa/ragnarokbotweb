using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class BunkerService : IBunkerService
    {
        private readonly IBunkerRepository _bunkerRepository;
        private readonly ILogger<BunkerService> _logger;

        public BunkerService(ILogger<BunkerService> logger, IBunkerRepository bunkerRepository)
        {
            _logger = logger;
            _bunkerRepository = bunkerRepository;
        }

        public async Task UpdateBunkerState(ScumServer server, string sector, bool locked, TimeSpan activation)
        {
            var bunker = await _bunkerRepository.FindOneWithServerAsync(b => b.Sector == sector && b.ScumServer.Id == server.Id);
            bunker ??= new(sector);
            bunker.ScumServer = server;
            bunker.Locked = locked;
            bunker.Available = DateTime.Now.Add(activation);

            await _bunkerRepository.CreateOrUpdateAsync(bunker);
            await _bunkerRepository.SaveAsync();
        }
    }
}
