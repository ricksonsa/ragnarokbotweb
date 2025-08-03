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

        public async Task<List<Bunker>> FindBunkersByServer(long serverId)
        {
            return await _bunkerRepository.FindWithServerAsync(bunker => bunker.ScumServer.Id == serverId);
        }

        public async Task UpdateBunkerState(ScumServer server, string sector, bool locked, TimeSpan activation)
        {
            var bunker = await _bunkerRepository.FindOneWithServerAsync(b => b.Sector == sector && b.ScumServer.Id == server.Id);
            bunker ??= new(sector);
            bunker.Locked = locked;
            bunker.Available = DateTime.UtcNow.Add(activation);
            bunker.ScumServer ??= server;

            try
            {
                await _bunkerRepository.CreateOrUpdateAsync(bunker);
                await _bunkerRepository.SaveAsync();
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}
