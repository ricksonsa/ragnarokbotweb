using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PlayerService> _logger;
        private readonly ICacheService _cacheService;

        public PlayerService(IUnitOfWork uow, ILogger<PlayerService> logger, ICacheService cacheService)
        {
            _uow = uow;
            _logger = logger;
            _cacheService = cacheService;
        }

        public bool IsPlayerConnected(string steamId64) => _cacheService.GetConnectedPlayers().ContainsKey(steamId64);

        public List<Player> OnlinePlayers()
        {
            return _cacheService.GetConnectedPlayers().Values.ToList();
        }

        public async Task<List<Player>> OfflinePlayers()
        {
            var allUsers = (await _uow.Users.ToListAsync()).Select(user => new Player
            {
                Name = user.Name,
                SteamID = user.SteamId64,
                AccountBalance = user.Money ?? 0,
                Fame = user.Fame ?? 0,
                SteamName = user.SteamName,
                GoldBalance = user.Gold ?? 0,
                X = user.X ?? 0,
                Y = user.Y ?? 0,
                Z = user.Z ?? 0,

            }).ToList();
            var values = _cacheService.GetConnectedPlayers().Values.ToList();
            return allUsers.ExceptBy(values.Select(v => v.SteamID), u => u.SteamID).ToList();
        }

        public void ResetPlayersConnection()
        {
            _cacheService.ClearConnectedPlayers();
        }

        public async Task PlayerConnected(string steamId64, string scumId, string name)
        {
            var user = await _uow.Users.FirstOrDefaultAsync(user => user.SteamId64 == steamId64);

            user ??= new();
            user.SteamId64 = steamId64;
            user.ScumId = scumId;
            user.Name = name;

            if (user.Id == 0)
            {
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

            _cacheService.GetCommandQueue().Enqueue(Command.ListPlayers());
        }

        public Player? PlayerDisconnected(string steamId64)
        {
            if (_cacheService.GetConnectedPlayers().Remove(steamId64, out var player))
            {
                return player;
            }

            return null;
        }
    }
}
