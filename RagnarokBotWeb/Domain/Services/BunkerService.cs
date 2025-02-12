using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class BunkerService : IBunkerService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<BunkerService> _logger;

        public BunkerService(IUnitOfWork uow, ILogger<BunkerService> logger)
        {
            _logger = logger;
            _uow = uow;
        }

        public async Task UpdateBunkerState(string sector, bool locked, TimeSpan activation)
        {
            var bunker = await _uow.Bunkers.FirstOrDefaultAsync(b => b.Sector == sector);
            if (bunker is null)
            {
                _logger.LogError($"Bunker {sector} not found");
                return;
            }

            bunker.Locked = locked;
            bunker.Available = DateTime.Now.Add(activation);

            _uow.Bunkers.Update(bunker);
            await _uow.SaveAsync();
        }
    }
}
