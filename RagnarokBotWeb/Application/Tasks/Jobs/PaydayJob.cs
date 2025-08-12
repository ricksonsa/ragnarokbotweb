using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;


public class PaydayJob(
    ILogger<PaydayJob> logger,
    IScumServerRepository scumServerRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {

        logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

        try
        {
            var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);

            if (server.CoinAwardPeriodically > 0)
            {
                var onlinePlayers = cacheService.GetConnectedPlayers(server.Id);
                foreach (var onlinePlayer in onlinePlayers)
                {
                    var player = await unitOfWork.Players
                        .Include(player => player.ScumServer)
                        .Include(player => player.Vips)
                        .FirstOrDefaultAsync(player => player.ScumServerId == server.Id && player.SteamId64 == onlinePlayer.SteamID);

                    if (player is null) continue;

                    var amount = server.CoinAwardPeriodically;
                    if (player.IsVip() && server.VipCoinAwardPeriodically > server.CoinAwardPeriodically)
                        amount = server.VipCoinAwardPeriodically;

                    await unitOfWork.AppDbContext.Database.ExecuteSqlRawAsync("SELECT addcoinstoplayer({0}, {1})", player.Id, amount);
                }
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception)
        {

            throw;
        }

    }
}