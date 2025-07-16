using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class PlayerService : BaseService, IPlayerService
    {
        private readonly ILogger<PlayerService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IPlayerRepository _playerRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        public PlayerService(
            IHttpContextAccessor contextAccessor,
            ILogger<PlayerService> logger,
            ICacheService cacheService,
            IPlayerRepository playerRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IServiceProvider serviceProvider) : base(contextAccessor)
        {
            _logger = logger;
            _cacheService = cacheService;
            _playerRepository = playerRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _serviceProvider = serviceProvider;
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
            var content = page.Content.Select(_mapper.Map<PlayerDto>);
            foreach (var player in content)
            {
                player.Online = _cacheService.GetConnectedPlayers(serverId.Value).Any(connectedPlayer => connectedPlayer.SteamID == player.SteamId64);
            }
            return new Page<PlayerDto>(content, page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
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
            var allUsers = (await _playerRepository.GetAllByServerId(serverId)).Select(user => new ScumPlayer
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
            var player = await _playerRepository.FindOneWithServerAsync(p => p.SteamId64 == steamId64 && p.ScumServer.Id == server.Id);

            player ??= new();
            player.SteamId64 = steamId64;
            player.ScumId = scumId;
            player.Name = name;
            player.ScumServer = (await _scumServerRepository.FindByIdAsync(server.Id))!;

            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();

            if (!_cacheService.GetCommandQueue(server.Id).Any(command => command.Values.Any(cv => cv.Type == Shared.Enums.ECommandType.Delivery)))
            {
                var command = new BotCommand();
                command.ListPlayers();
                _cacheService.GetCommandQueue(server.Id).Enqueue(command);
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

            using var scope = _serviceProvider.CreateScope(); // inject IServiceProvider
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var player in players)
            {
                var user = await ctx.Players
                .Include(p => p.ScumServer)
                .FirstOrDefaultAsync(p => p.SteamId64 == player.SteamID && p.ScumServerId == serverId);
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
                if (user.Id == 0)
                {
                    await ctx.Players.AddAsync(user);
                }
                else
                {
                    ctx.Players.Update(user);
                }

                await ctx.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Player>> GetAllUsersAsync()
        {
            return await _playerRepository.GetAllAsync();
        }

        public async Task<Player?> FindBySteamId64Async(string steamId64, long serverId)
        {
            return await _playerRepository.FindOneWithServerAsync(player => player.SteamId64 == steamId64 && player.ScumServerId == serverId);
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

        public async Task UpdatePlayerNameAsync(Entities.ScumServer server, string steamId64, string scumId, string name)
        {
            var player = await FindBySteamId64Async(steamId64, server.Id);

            player ??= new();
            player.SteamId64 = steamId64;
            player.ScumId = scumId;
            player.Name = name;
            player.ScumServer = server;
            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();
        }
    }
}
