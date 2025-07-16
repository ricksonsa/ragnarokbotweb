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

        public Task<Bot?> FindByOnlineScumServerId(long serverId)
        {
            return _appDbContext.Bots.Include(bot => bot.ScumServer)
                .FirstOrDefaultAsync(bot => bot.ScumServer.Id == serverId && bot.Active && bot.State == EBotState.Online);
        }

        public Task<List<Bot>> FindByServerIdOnlineAndLastInteraction(long id)
        {
            var date = DateTime.UtcNow.AddMinutes(-1);
            return _appDbContext.Bots
                .Include(bot => bot.ScumServer)
                .Where(bot => bot.ScumServer.Id == id && bot.State == EBotState.Online && bot.LastInteracted <= date)
                .ToListAsync();
        }

        public Task<List<Bot>> FindByServerIdOnlineAndLastInteraction(ulong guildId)
        {
            var date = DateTime.UtcNow.AddMinutes(-1);
            return _appDbContext.Bots
                .Include(bot => bot.ScumServer)
                .Include(bot => bot.ScumServer.Guild)
                .Where(bot => bot.ScumServer.Guild != null && bot.ScumServer.Guild.DiscordId == guildId && bot.State == EBotState.Online && bot.LastInteracted <= date)
                .ToListAsync();
        }

        public Task<List<Bot>> FindOnlineBotByGuild(ulong guildId)
        {
            return _appDbContext.Bots
                .Include(bot => bot.ScumServer)
                .Include(bot => bot.ScumServer.Guild)
                .Where(bot => bot.ScumServer.Guild != null && bot.ScumServer.Guild.DiscordId == guildId && bot.State == EBotState.Online)
                .ToListAsync();
        }
    }
}
