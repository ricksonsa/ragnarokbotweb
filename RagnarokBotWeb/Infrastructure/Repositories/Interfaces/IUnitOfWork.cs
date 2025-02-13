using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        DbSet<User> Users { get; }
        DbSet<Lockpick> Lockpicks { get; }
        DbSet<Bunker> Bunkers { get; }
        DbSet<Reader> Readings { get; }
        DbSet<Kill> Kills { get; }
        Task SaveAsync();
    }
}
