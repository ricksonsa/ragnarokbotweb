using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Configuration
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Lockpick> Lockpicks { get; set; }
        public DbSet<Bunker> Bunkers { get; set; }
        public DbSet<Reader> Readings { get; set; }
        public DbSet<ReaderPointer> ReaderPointers { get; set; }
        public DbSet<Bot> Bots { get; set; }
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
        public DbSet<Command> Commands { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<ScumServer> ScumServers { get; set; }
        public DbSet<Ftp> Ftps { get; set; }
        public DbSet<ScheduledTask> ScheduledTasks { get; set; }
        public DbSet<PlayerRegister> PlayerRegisters { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // TODO: Load from env config
            options.UseNpgsql("Host=localhost;Database=ragnarokbot;Username=postgres;Password=ragnarokbot");
            //options.UseSqlite("Data Source=app.db");
        }

        public void MigrateDatabase()
        {
            Database.Migrate(); // Apply pending migrations automatically
        }
    }
}
