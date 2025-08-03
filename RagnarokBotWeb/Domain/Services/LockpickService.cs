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
            _uow.ScumServers.Attach(lockpick.ScumServer);
            await _uow.Lockpicks.AddAsync(lockpick);
            await _uow.SaveAsync();
            return lockpick;
        }
    }
}
