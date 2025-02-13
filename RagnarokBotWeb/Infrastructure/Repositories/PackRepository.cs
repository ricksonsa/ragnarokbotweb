using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class PackRepository : Repository<Pack>, IPackRepository
    {
        private readonly AppDbContext _appDbContext;
        public PackRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public override async Task<Pack> GetByIdAsync(long id)
        {
            return await _appDbContext.Packs
                .Include(pack => pack.PackItems)
                .ThenInclude(packItem => packItem.Item)
                .FirstOrDefaultAsync(pack => pack.Id == id);
        }

        public override async Task<IEnumerable<Pack>> GetAllAsync()
        {
            return await _appDbContext.Packs
               .Include(pack => pack.PackItems)
               .ThenInclude(packItem => packItem.Item)
               .ToListAsync();
        }

    }
}
