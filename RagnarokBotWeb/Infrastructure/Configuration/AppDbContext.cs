using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Infrastructure.Configuration
{
    public class AppDbContext : DbContext
    {
        private readonly string _connectionString;
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
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Config> Config { get; set; }
        public DbSet<CustomTask> CustomTasks { get; set; }
        public DbSet<Taxi> Taxis { get; set; }
        public DbSet<TaxiTeleport> TaxiTeleports { get; set; }
        public DbSet<Exchange> Exchanges { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {

            if (!options.IsConfigured)
            {
                options.UseNpgsql(_connectionString);
            }
            //dotnet ef database update --connection "connection string"
            //options.UseNpgsql("Host=localhost;Database=ragnarokbot;Username=postgres;Password=ragnarokbot");
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
                            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(), // store as UTC
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)              // read as UTC
                        ));
                    }
                }
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType, builder =>
                    {
                        builder.Property("CreateDate").HasColumnType("timestamptz");
                        builder.Property("UpdateDate").HasColumnType("timestamptz");
                    });
                }
            }

            modelBuilder.Entity<Ban>()
                .Property(x => x.ExpirationDate).HasColumnType("timestamptz");

            modelBuilder.Entity<Silence>()
                .Property(x => x.ExpirationDate).HasColumnType("timestamptz");

            modelBuilder.Entity<DiscordRole>()
                .Property(x => x.ExpirationDate).HasColumnType("timestamptz");

            modelBuilder.Entity<Vip>()
                .Property(x => x.ExpirationDate).HasColumnType("timestamptz");

            modelBuilder.Entity<Payment>()
                .Property(x => x.ConfirmDate).HasColumnType("timestamptz");

            modelBuilder.Entity<Bunker>()
                .Property(x => x.Available).HasColumnType("timestamptz");

            modelBuilder.Entity<Player>()
                .Property(x => x.LastLoggedIn).HasColumnType("timestamptz");

            modelBuilder.Entity<ReaderPointer>()
                .Property(x => x.FileDate).HasColumnType("timestamptz");

            modelBuilder.Entity<ReaderPointer>()
                .Property(x => x.LastUpdated).HasColumnType("timestamptz");

            modelBuilder.Entity<Lockpick>()
                .Property(x => x.AttemptDate).HasColumnType("timestamptz");

            modelBuilder.Entity<CustomTask>()
                .Property(x => x.ExpireAt).HasColumnType("timestamptz");

            modelBuilder.Entity<CustomTask>()
                .Property(x => x.LastRunned).HasColumnType("timestamptz");

            modelBuilder.Entity<Uav>()
                .HasOne(u => u.ScumServer)
                .WithOne(s => s.Uav)
                .HasForeignKey<ScumServer>(s => s.UavId);

            modelBuilder.Entity<Exchange>()
                .HasOne(u => u.ScumServer)
                .WithOne(s => s.Exchange)
                .HasForeignKey<ScumServer>(s => s.ExchangeId);

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

            modelBuilder.Entity<TaxiTeleport>()
                .HasOne(tt => tt.Teleport)
                .WithMany()
                .HasForeignKey(tt => tt.TeleportId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void MigrateDatabase()
        {
            Database.Migrate(); // Apply pending migrations automatically
        }
    }
}
