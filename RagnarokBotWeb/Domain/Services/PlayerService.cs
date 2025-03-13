using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class PlayerService : BaseService, IPlayerService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PlayerService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IPlayerRepository _playerRepository;
        private readonly IMapper _mapper;

        public PlayerService(
            IHttpContextAccessor contextAccessor,
            IUnitOfWork uow,
            ILogger<PlayerService> logger,
            ICacheService cacheService,
            IPlayerRepository playerRepository,
            IMapper mapper) : base(contextAccessor)
        {
            _uow = uow;
            _logger = logger;
            _cacheService = cacheService;
            _playerRepository = playerRepository;
            _mapper = mapper;
        }

        public async Task<PlayerDto> GetPlayer(long id)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null) throw new NotFoundException("Player not found");
            if (player.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Unauthorized server");

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<Page<PlayerDto>> GetPlayers(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");
            var page = await _playerRepository.GetPageByServerId(paginator, serverId.Value, filter);
            return new Page<PlayerDto>(page.Content.Select(_mapper.Map<PlayerDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public bool IsPlayerConnected(string steamId64, long? serverId)
        {
            if (serverId.HasValue)
            {
                return _cacheService.GetConnectedPlayers(serverId.Value).Any(player => player.SteamID.Equals(steamId64));
            }

            serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server");

            return _cacheService.GetConnectedPlayers(serverId.Value).Any(player => player.SteamID.Equals(steamId64));
        }

        public List<ScumPlayer> OnlinePlayers(long serverId) => _cacheService.GetConnectedPlayers(serverId);

        public async Task<List<ScumPlayer>> OfflinePlayers(long serverId)
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
            var values = _cacheService.GetConnectedPlayers(serverId);
            return allUsers.ExceptBy(values.Select(v => v.SteamID), u => u.SteamID).ToList();
        }

        public void ResetPlayersConnection(long? serverId)
        {

            if (serverId.HasValue)
            {
                _cacheService.ClearConnectedPlayers(serverId.Value);
            }

            serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server");

            _cacheService.ClearConnectedPlayers(serverId.Value);
        }

        public async Task PlayerConnected(Entities.ScumServer server, string steamId64, string scumId, string name)
        {
            var player = await _uow.Players.FirstOrDefaultAsync(user => user.SteamId64 == steamId64);

            player ??= new();
            player.SteamId64 = steamId64;
            player.ScumId = scumId;
            player.Name = name;
            player.ScumServer = server;

            if (player.Id == 0)
            {
                await _uow.Players.AddAsync(player);
                await _uow.SaveAsync();
                _logger.Log(LogLevel.Information, $"New User Connected {steamId64} {name}({scumId})");
            }
            else
            {
                _uow.Players.Update(player);
                await _uow.SaveAsync();
                _logger.Log(LogLevel.Information, $"Registered User Connected {steamId64} {name}({scumId})");
            }

            if (!_cacheService.GetCommandQueue(server.Id).Any(command => command.Type == Shared.Enums.ECommandType.ListPlayers))
            {
                _cacheService.GetCommandQueue(server.Id).Enqueue(BotCommand.ListPlayers());
            }
        }

        public ScumPlayer? PlayerDisconnected(long serverId, string steamId64)
        {
            var player = _cacheService.GetConnectedPlayers(serverId).FirstOrDefault(p => p.SteamID == steamId64);
            if (player is not null)
            {
                _cacheService.GetConnectedPlayers(serverId).Remove(player);
            }

            return null;
        }

        public async Task UpdateFromScumPlayers(long serverId, List<ScumPlayer>? players)
        {
            if (players is null) players = _cacheService.GetConnectedPlayers(serverId);
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

        public Task<Player?> FindBySteamId64Async(string steamId64)
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
