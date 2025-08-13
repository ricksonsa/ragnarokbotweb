using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Handlers
{
    public class PlayerCoinManager(IUnitOfWork uow)
    {
        public async Task AddCoinsBySteamIdAsync(string steamId, long serverId, long amount)
        {
            await uow.AppDbContext.Database.ExecuteSqlRawAsync("SELECT addcoinstoplayerbysteamid({0}, {1}, {2})", steamId, serverId, amount);
        }

        public async Task RemoveCoinsBySteamIdAsync(string steamId, long serverId, long amount)
        {
            await uow.AppDbContext.Database.ExecuteSqlRawAsync("SELECT reducetoplayerbysteamid({0}, {1}, {2})", steamId, serverId, amount);
        }

        public async Task AddCoinsByPlayerId(long playerId, long amount)
        {

            await uow.AppDbContext.Database.ExecuteSqlRawAsync("SELECT addcoinstoplayer({0}, {1})", playerId, amount);
        }

        public async Task RemoveCoinsByPlayerId(long playerId, long amount)
        {
            await uow.AppDbContext.Database.ExecuteSqlRawAsync("SELECT reducecoinstoplayer({0}, {1})", playerId, amount);
        }
    }
}
