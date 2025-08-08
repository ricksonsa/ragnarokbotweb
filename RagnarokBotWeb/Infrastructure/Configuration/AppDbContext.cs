using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Configuration
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Lockpick> Lockpicks { get; set; }
        public DbSet<Bunker> Bunkers { get; set; }
        public DbSet<ReaderPointer> ReaderPointers { get; set; }
        public DbSet<Kill> Kills { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Pack> Packs { get; set; }
        public DbSet<PackItem> PackItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Button> Buttons { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<ButtonTemplate> ButtonTemplates { get; set; }
        public DbSet<ChannelTemplate> ChannelTemplates { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<ScumServer> ScumServers { get; set; }
        public DbSet<Ftp> Ftps { get; set; }
        public DbSet<ScheduledTask> ScheduledTasks { get; set; }
        public DbSet<PlayerRegister> PlayerRegisters { get; set; }
        public DbSet<Vip> Vips { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<Silence> Silences { get; set; }
        public DbSet<Warzone> Warzones { get; set; }
        public DbSet<WarzoneItem> WarzoneItems { get; set; }
        public DbSet<Teleport> Teleports { get; set; }
        public DbSet<WarzoneSpawn> WarzoneSpawns { get; set; }
        public DbSet<WarzoneTeleport> WarzoneTeleports { get; set; }
        public DbSet<DiscordRole> DiscordRoles { get; set; }
        public DbSet<Uav> Uavs { get; set; }
        public DbSet<Subscription> Subscription { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public AppDbContext()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // TODO: Load from env config
            options.UseNpgsql("Host=localhost;Database=ragnarokbot;Username=postgres;Password=ragnarokbot");
            //.EnableSensitiveDataLogging() // Optional: shows parameters in logs
            //.LogTo(Log.Logger.Information, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information);
            //options.UseSqlite("Data Source=app.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }

            modelBuilder.Entity<Uav>()
            .HasOne(u => u.ScumServer)
            .WithOne(s => s.Uav)
            .HasForeignKey<ScumServer>(s => s.UavId);

            modelBuilder.Entity<PackItem>()
            .HasOne(wi => wi.Item)
            .WithMany()
            .HasForeignKey(wi => wi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarzoneItem>()
            .HasOne(wi => wi.Item)
            .WithMany()
            .HasForeignKey(wi => wi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarzoneSpawn>()
            .HasOne(wi => wi.Teleport)
            .WithMany()
            .HasForeignKey(wi => wi.TeleportId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarzoneTeleport>()
            .HasOne(wi => wi.Teleport)
            .WithMany()
            .HasForeignKey(wi => wi.TeleportId)
            .OnDelete(DeleteBehavior.Cascade);
        }

        public void MigrateDatabase()
        {
            Database.Migrate(); // Apply pending migrations automatically
        }
    }
}
