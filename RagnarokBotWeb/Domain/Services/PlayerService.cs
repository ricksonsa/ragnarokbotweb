using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PlayerService> _logger;

        public PlayerService(IUnitOfWork uow, ILogger<PlayerService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public bool IsPlayerConnected(string steamId64) => GameLoadStateHostedService.ConnectedUsers.ContainsKey(steamId64);

        public List<User> OnlinePlayers()
        {
            return GameLoadStateHostedService.ConnectedUsers.Values.ToList();
        }

        public async Task PlayerConnected(string steamId64, string scumId, string name)
        {
            var user = await _uow.Users.FirstOrDefaultAsync(user => user.SteamId64 == steamId64);

            user ??= new();
            user.SteamId64 = steamId64;
            user.ScumId = scumId;
            user.Presence = "online";
            user.Name = name;

            if (user.Id == 0)
            {
                user.CreateDate = DateTime.Now;
                await _uow.Users.AddAsync(user);
                await _uow.SaveAsync();
                _logger.Log(LogLevel.Information, $"New User Connected {steamId64} {name}({scumId})");
            }
            else
            {
                _uow.Users.Update(user);
                await _uow.SaveAsync();
                _logger.Log(LogLevel.Information, $"Registered User Connected {steamId64} {name}({scumId})");
            }

            GameLoadStateHostedService.ConnectedUsers.AddOrUpdate(steamId64, user, (key, oldValue) => oldValue);
        }

        public async Task<User> PlayerDisconnected(string steamId64)
        {
            GameLoadStateHostedService.ConnectedUsers.Remove(steamId64, out var user);

            if (user is null)
            {
                user = await _uow.Users.FirstOrDefaultAsync(user => user.SteamId64 == steamId64);
            }

            user!.Presence = "offline";
            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return user;
        }
    }
}
