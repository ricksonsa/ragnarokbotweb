using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Configuration
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            //options.UseNpgsql("Host=localhost;Database=ragnarokbot;Username=myuser;Password=mypassword");
            options.UseSqlite("Data Source=app.db");
        }

        public void MigrateDatabase()
        {
            Database.EnsureCreated();
            Database.Migrate(); // Apply pending migrations automatically
        }
    }
}
