using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual DbSet<T> DbSet()
        {
            return _dbSet;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T?> FindByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<Page<T>> GetPageAsync(Paginator paginator, IQueryable<T> query)
        {
            var count = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)paginator.PageSize);
            var result = await query.Skip((paginator.PageNumber - 1) * paginator.PageSize).Take(paginator.PageSize).ToListAsync();
            return new Page<T>(result, totalPages, count, paginator.PageNumber, paginator.PageSize);
        }

        public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(IList<T> entity)
        {
            await _dbSet.AddRangeAsync(entity);
        }

        public virtual void Update(T entity)
        {
            entity.UpdateDate = DateTime.UtcNow;
            _dbSet.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasAny(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);

        }

        public virtual async Task CreateOrUpdateAsync(T entity)
        {
            if (entity.IsTransitory())
            {
                await AddAsync(entity);
            }
            else
            {
                Update(entity);
            }
        }
    }
}
