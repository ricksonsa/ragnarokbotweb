using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class PackItemRepository : Repository<PackItem>, IPackItemRepository
    {
        private readonly AppDbContext _appDbContext;
        public PackItemRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public override Task AddAsync(PackItem entity)
        {
            _appDbContext.Items.Attach(entity.Item);
            return base.AddAsync(entity);
        }
    }
}
