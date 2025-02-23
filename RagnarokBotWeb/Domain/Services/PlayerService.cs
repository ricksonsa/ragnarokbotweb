using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application;
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
        private readonly IPlayerRepository _playerRepository;

        public PlayerService(
            IUnitOfWork uow,
            ILogger<PlayerService> logger,
            ICacheService cacheService, IPlayerRepository playerRepository)
        {
            _uow = uow;
            _logger = logger;
            _cacheService = cacheService;
            _playerRepository = playerRepository;
        }

        public bool IsPlayerConnected(string steamId64) => _cacheService.GetConnectedPlayers().ContainsKey(steamId64);

        public List<ScumPlayer> OnlinePlayers()
        {
            return _cacheService.GetConnectedPlayers().Values.ToList();
        }

        public async Task<List<ScumPlayer>> OfflinePlayers()
        {
            var allUsers = (await _uow.Players.ToListAsync()).Select(user => new ScumPlayer
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
            var user = await _uow.Players.FirstOrDefaultAsync(user => user.SteamId64 == steamId64);

            user ??= new();
            user.SteamId64 = steamId64;
            user.ScumId = scumId;
            user.Name = name;

            if (user.Id == 0)
            {
                await _uow.Players.AddAsync(user);
                await _uow.SaveAsync();
                _logger.Log(LogLevel.Information, $"New User Connected {steamId64} {name}({scumId})");
            }
            else
            {
                _uow.Players.Update(user);
                await _uow.SaveAsync();
                _logger.Log(LogLevel.Information, $"Registered User Connected {steamId64} {name}({scumId})");
            }

            _cacheService.GetCommandQueue().Enqueue(BotCommand.ListPlayers());
        }

        public ScumPlayer? PlayerDisconnected(string steamId64)
        {
            if (_cacheService.GetConnectedPlayers().Remove(steamId64, out var player))
            {
                return player;
            }

            return null;
        }

        public async Task UpdateFromScumPlayers(List<ScumPlayer>? players)
        {
            if (players is null) players = _cacheService.GetConnectedPlayers().Values.ToList();
            foreach (var player in players)
            {
                var user = await FindBySteamId64Async(player.SteamID);
                user ??= new();
                user.X = player.X;
                user.Y = player.Y;
                user.Z = player.Z;
                user.Name = player.Name;
                user.SteamId64 = player.SteamID;
                user.SteamName = player.SteamName;
                user.Gold = player.GoldBalance;
                user.Money = player.AccountBalance;
                user.Fame = player.Fame;
                _playerRepository.Update(user);
            }
            await _playerRepository.SaveAsync();
        }

        public async Task<IEnumerable<Player>> GetAllUsersAsync()
        {
            return await _playerRepository.GetAllAsync();
        }

        public Task<Player> FindBySteamId64Async(string steamId64)
        {
            return _playerRepository.FindOneAsync(user => user.SteamId64 == steamId64);
        }

        public async Task AddPlayerAsync(Player user)
        {
            await _playerRepository.AddAsync(user);
            await _playerRepository.SaveAsync();
        }

        public async Task UpdatePlayerAsync(Player user)
        {
            _playerRepository.Update(user);
            await _playerRepository.SaveAsync();
        }

        public async Task UpdatePlayerNameAsync(string steamId64, string name)
        {
            var user = await FindBySteamId64Async(steamId64);
            user.Name = name;
            _playerRepository.Update(user);
            await _playerRepository.SaveAsync();
        }
    }
}
