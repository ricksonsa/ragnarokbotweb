using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<User?> FindOneWithTenantAsync(Expression<Func<User, bool>> predicate)
        {
            return _context.Users.Include(user => user.Tenant).Where(predicate).FirstOrDefaultAsync();
        }

        public override Task AddAsync(User entity)
        {
            if (entity.Tenant is not null) _context.Tenants.Attach(entity.Tenant);
            return base.AddAsync(entity);
        }
    }
}
