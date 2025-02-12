using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ILockpickService
    {
        Task<Lockpick> AddLockpickAttemptAsync(Lockpick lockpick);
    }
}
