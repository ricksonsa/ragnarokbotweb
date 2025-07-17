using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        AppDbContext AppDbContext { get; }
        DbSet<Player> Players { get; }
        DbSet<Lockpick> Lockpicks { get; }
        DbSet<Bunker> Bunkers { get; }
        DbSet<ReaderPointer> ReaderPointers { get; }
        DbSet<Kill> Kills { get; }
        DbSet<Bot> Bots { get; }
        DbSet<Tenant> Tenants { get; }
        DbSet<ScumServer> ScumServers { get; }
        DbSet<ScheduledTask> ScheduledTasks { get; }
        DbSet<Ftp> Ftps { get; }
        DbSet<Vip> Vips { get; }
        DbSet<Ban> Bans { get; }
        DbSet<Silence> Silences { get; }
        DbSet<Warzone> Warzones { get; }
        DbSet<WarzoneItem> WarzoneItems { get; }
        DbSet<Teleport> Teleports { get; }
        DbSet<WarzoneSpawn> WarzoneSpawns { get; }
        DbSet<WarzoneTeleport> WarzoneTeleports { get; }


        Task SaveAsync();
    }
}
