using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class ReaderRepository : Repository<Reader>, IReaderRepository
    {
        public ReaderRepository(AppDbContext context) : base(context) { }
    }
}
