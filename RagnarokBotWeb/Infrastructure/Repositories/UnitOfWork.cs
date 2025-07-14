using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private AppDbContext _context;
        public DbSet<Player> Players { get; }
        public DbSet<Lockpick> Lockpicks { get; }
        public DbSet<Bunker> Bunkers { get; }
        public DbSet<Reader> Readings { get; }
        public DbSet<ReaderPointer> ReaderPointers { get; }
        public DbSet<Kill> Kills { get; }
        public DbSet<Bot> Bots { get; }
        public DbSet<Tenant> Tenants { get; }
        public DbSet<ScumServer> ScumServers { get; }
        public DbSet<ScheduledTask> ScheduledTasks { get; }
        public DbSet<Ftp> Ftps { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Players = context.Players;
            Lockpicks = context.Lockpicks;
            Bunkers = context.Bunkers;
            ReaderPointers = context.ReaderPointers;
            Kills = context.Kills;
            Bots = context.Bots;
            Tenants = context.Tenants;
            ScumServers = context.ScumServers;
            Ftps = context.Ftps;
            ScheduledTasks = context.ScheduledTasks;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
