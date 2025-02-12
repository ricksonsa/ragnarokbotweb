using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private AppDbContext _context;
        public DbSet<User> Users { get; }
        public DbSet<Lockpick> Lockpicks { get; }
        public DbSet<Bunker> Bunkers { get; }
        public DbSet<Reader> Readings { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = context.Users;
            Lockpicks = context.Lockpicks;
            Bunkers = context.Bunkers;
            Readings = context.Readings;
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
