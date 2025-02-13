using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Configuration
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Lockpick> Lockpicks { get; set; }
        public DbSet<Bunker> Bunkers { get; set; }
        public DbSet<Reader> Readings { get; set; }
        public DbSet<Bot> Bots { get; set; }
        public DbSet<Kill> Kills { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Pack> Packs { get; set; }
        public DbSet<PackItem> PackItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            //options.UseNpgsql("Host=localhost;Database=ragnarokbot;Username=myuser;Password=mypassword");
            options.UseSqlite("Data Source=app.db");
        }

        public void MigrateDatabase()
        {
            Database.Migrate(); // Apply pending migrations automatically
        }
    }
}
