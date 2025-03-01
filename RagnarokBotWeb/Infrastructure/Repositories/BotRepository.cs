using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class BotRepository : Repository<Bot>, IBotRepository
    {
        private readonly AppDbContext _appDbContext;
        public BotRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public Task<Bot?> FindByScumServerId(long id)
        {
            return _appDbContext.Bots.Include(bot => bot.ScumServer).FirstOrDefaultAsync(bot => bot.ScumServer.Id == id && bot.Active);
        }
    }
}
