using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        DbSet<T> DbSet();
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> FindByIdAsync(long id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate);
        Task<bool> HasAny(Expression<Func<T, bool>> predicate);
        Task CreateOrUpdateAsync(T entity);
        Task AddAsync(T entity);
        Task AddRangeAsync(IList<T> entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveAsync();
    }
}
