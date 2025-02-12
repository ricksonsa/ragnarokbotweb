using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.HostedServices
{
    public class GameLoadStateHostedService : IHostedService
    {
        public static ConcurrentDictionary<string, User> ConnectedUsers = [];

        private readonly ILogger<GameLoadStateHostedService> _logger;
        private readonly IServiceProvider _services;

        public GameLoadStateHostedService(
            ILogger<GameLoadStateHostedService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GameLoadStateHostedService loading game state...");

            using (var scope = _services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var onlineUsers = uow.Users.Where(user => user.Presence == "online");
                foreach (var user in onlineUsers)
                {
                    ConnectedUsers.AddOrUpdate(user.SteamId64!, user, (key, oldValue) => oldValue);
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
