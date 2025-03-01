using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        DbSet<Player> Players { get; }
        DbSet<Lockpick> Lockpicks { get; }
        DbSet<Bunker> Bunkers { get; }
        DbSet<Reader> Readings { get; }
        DbSet<Kill> Kills { get; }
        DbSet<Bot> Bots { get; }
        DbSet<Tenant> Tenants { get; }
        DbSet<ScumServer> ScumServers { get; }
        DbSet<ScheduledTask> ScheduledTasks { get; }
        DbSet<Ftp> Ftps { get; }
        Task SaveAsync();
    }
}
