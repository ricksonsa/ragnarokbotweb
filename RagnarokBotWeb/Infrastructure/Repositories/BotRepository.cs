using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class BotRepository : Repository<Bot>, IBotRepository
    {
        private readonly AppDbContext _appDbContext;
        public BotRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public Task<List<Bot>> FindActiveBotsByServerId(long id)
        {
            return _appDbContext.Bots
            .Include(bot => bot.ScumServer)
              .Where(bot => bot.ScumServer.Id == id && bot.State == EBotState.Online)
              .ToListAsync();
        }

        public Task<Bot?> FindByScumServerId(long id)
        {
            return _appDbContext.Bots.Include(bot => bot.ScumServer).FirstOrDefaultAsync(bot => bot.ScumServer.Id == id && bot.Active);
        }

        public Task<List<Bot>> FindByServerIdOnlineAndLastInteraction(long id)
        {
            var date = DateTime.Now.AddMinutes(-2);
            return _appDbContext.Bots
                .Include(bot => bot.ScumServer)
                .Where(bot => bot.ScumServer.Id == id && bot.State == EBotState.Online && bot.LastInteracted <= date)
                .ToListAsync();
        }
    }
}
