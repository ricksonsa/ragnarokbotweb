using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class LockpickService : ILockpickService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<LockpickService> _logger;

        public LockpickService(IUnitOfWork uow, ILogger<LockpickService> logger)
        {
            _logger = logger;
            _uow = uow;
        }

        public async Task<Lockpick> AddLockpickAttemptAsync(Lockpick lockpick)
        {
            _logger.Log(LogLevel.Information, "Adding new lockpick attempt from steamId: {}", lockpick.SteamId64);
            var user = await _uow.Players.FirstOrDefaultAsync(user => user.SteamId64 == lockpick.SteamId64);

            if (lockpick.ScumServer is not null)
            {
                var tracked = _uow.AppDbContext.ChangeTracker.Entries<ScumServer>()
                    .FirstOrDefault(e => e.Entity.Id == lockpick.ScumServer.Id);

                if (tracked == null)
                {
                    _uow.AppDbContext.ScumServers.Attach(lockpick.ScumServer);
                }
                else
                {
                    lockpick.ScumServer = tracked.Entity;
                }
            }

            await _uow.Lockpicks.AddAsync(lockpick);
            await _uow.SaveAsync();
            return lockpick;
        }
    }
}
