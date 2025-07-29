using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private AppDbContext _context;

        public AppDbContext AppDbContext { get => _context; }
        public DbSet<Player> Players { get; }
        public DbSet<Lockpick> Lockpicks { get; }
        public DbSet<Bunker> Bunkers { get; }
        public DbSet<ReaderPointer> ReaderPointers { get; }
        public DbSet<Kill> Kills { get; }
        public DbSet<Bot> Bots { get; }
        public DbSet<Tenant> Tenants { get; }
        public DbSet<ScumServer> ScumServers { get; }
        public DbSet<ScheduledTask> ScheduledTasks { get; }
        public DbSet<Ftp> Ftps { get; }
        public DbSet<Vip> Vips { get; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<Silence> Silences { get; set; }
        public DbSet<Warzone> Warzones { get; set; }
        public DbSet<WarzoneItem> WarzoneItems { get; set; }
        public DbSet<Teleport> Teleports { get; set; }
        public DbSet<WarzoneSpawn> WarzoneSpawns { get; set; }
        public DbSet<WarzoneTeleport> WarzoneTeleports { get; set; }
        public DbSet<DiscordRole> DiscordRoles { get; set; }
        public IDbContextFactory<AppDbContext> _dbContextFactory { get; }

        public UnitOfWork(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _context = dbContextFactory.CreateDbContext();
            Players = _context.Players;
            Lockpicks = _context.Lockpicks;
            Bunkers = _context.Bunkers;
            ReaderPointers = _context.ReaderPointers;
            Kills = _context.Kills;
            Bots = _context.Bots;
            Tenants = _context.Tenants;
            ScumServers = _context.ScumServers;
            Ftps = _context.Ftps;
            ScheduledTasks = _context.ScheduledTasks;
            Vips = _context.Vips;
            Bans = _context.Bans;
            Silences = _context.Silences;
            Silences = _context.Silences;
            WarzoneItems = _context.WarzoneItems;
            Teleports = _context.Teleports;
            WarzoneTeleports = _context.WarzoneTeleports;
            WarzoneSpawns = _context.WarzoneSpawns;
            Teleports = _context.Teleports;
            Warzones = _context.Warzones;
            DiscordRoles = _context.DiscordRoles;
            _dbContextFactory = dbContextFactory;
        }

        public AppDbContext CreateDbContext()
        {
            _context = _dbContextFactory.CreateDbContext();
            return _context;
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
