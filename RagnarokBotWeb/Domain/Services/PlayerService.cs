using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Domain.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IUnitOfWork _uow;
        public static ConcurrentDictionary<string, User> ConnectedUsers = [];
        private readonly ILogger<PlayerService> _logger;

        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public PlayerService(IUnitOfWork uow, ILogger<PlayerService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public bool IsPlayerConnected(string steamId64) => ConnectedUsers.ContainsKey(steamId64);

        public async Task PlayerConnected(string steamId64, string scumId, string name)
        {
            var user = await _uow.Users.FirstOrDefaultAsync(user => user.SteamId64 == steamId64);

            user ??= new();
            user.SteamId64 = steamId64;
            user.ScumId = scumId;
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

            ConnectedUsers.AddOrUpdate(steamId64, user, (key, oldValue) => oldValue);
        }

        public User PlayerDisconnected(string steamId64)
        {
            ConnectedUsers.Remove(steamId64, out var user);
            return user!;
        }
    }
}
