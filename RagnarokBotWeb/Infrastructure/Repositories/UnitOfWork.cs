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

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = context.Users;
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
