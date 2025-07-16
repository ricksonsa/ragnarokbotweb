using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public class WarzoneRepository : Repository<Warzone>, IWarzoneRepository
    {
        public WarzoneRepository(AppDbContext context) : base(context)
        {
        }
    }
}
