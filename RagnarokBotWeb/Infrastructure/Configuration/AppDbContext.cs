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
