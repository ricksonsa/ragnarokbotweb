using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class DiscordRoleExpireJob : AbstractJob, IJob
    {
        private readonly ILogger<DiscordRoleExpireJob> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscordService _discordService;

        public DiscordRoleExpireJob(
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            IDiscordService discordService,
            ILogger<DiscordRoleExpireJob> logger) : base(scumServerRepository)
        {
            _unitOfWork = unitOfWork;
            _discordService = discordService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(DiscordRoleExpireJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            var players = _unitOfWork.Players
                .Include(player => player.DiscordRoles)
                .Include(player => player.ScumServer)
                .Include(player => player.ScumServer.Guild)
                .Where(player =>
                    player.ScumServer != null
                    && player.ScumServer.Id == server.Id
                    && player.ScumServer.Guild != null
                    && player.DiscordId.HasValue
                    && player.DiscordRoles.Any(role => !role.Indefinitely && role.ExpirationDate.HasValue && role.ExpirationDate.Value.Date < DateTime.UtcNow.Date));

            foreach (var player in players)
            {
                foreach (var role in player.DiscordRoles)
                {
                    try
                    {
                        await _discordService.RemoveUserRoleAsync(player.ScumServer!.Guild!.DiscordId, player.DiscordId!.Value, role.DiscordId);
                        _unitOfWork.DiscordRoles.Remove(role);
                        await _unitOfWork.SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Could not remove user role with id {} with exception {}", role.DiscordId, ex.Message);
                    }
                }
            }
        }
    }
}
