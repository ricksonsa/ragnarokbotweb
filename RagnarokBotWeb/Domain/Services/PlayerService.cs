using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Crosscutting.Utils;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;
using System.Globalization;
using System.Text;
using static RagnarokBotWeb.Application.Tasks.Jobs.KillRankJob;
using static RagnarokBotWeb.Application.Tasks.Jobs.LockpickRankJob;

namespace RagnarokBotWeb.Domain.Services
{
    public class PlayerService : BaseService, IPlayerService
    {
        private readonly ILogger<PlayerService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IBotService _botService;
        private readonly IPlayerRepository _playerRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IDiscordService _discordService;
        private readonly IFtpService _ftpService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PlayerService(
            IHttpContextAccessor contextAccessor,
            ILogger<PlayerService> logger,
            ICacheService cacheService,
            IPlayerRepository playerRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            IDiscordService discordService,
            IBotService botService,
            IFtpService ftpService) : base(contextAccessor)
        {
            _logger = logger;
            _cacheService = cacheService;
            _playerRepository = playerRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
            _discordService = discordService;
            _botService = botService;
            _ftpService = ftpService;
        }

        public async Task<PlayerDto> GetPlayer(long id)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null) throw new NotFoundException("Player not found");

            var playerDto = _mapper.Map<PlayerDto>(player);
            playerDto.Online = _cacheService.GetConnectedPlayers(player.ScumServerId)
                .Any(p => p.SteamID == playerDto.SteamId64);

            return playerDto;
        }

        public async Task<Page<PlayerDto>> GetPlayers(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _playerRepository.GetPageByServerId(paginator, serverId!.Value, filter);
            var content = page.Content.Select(_mapper.Map<PlayerDto>);
            foreach (var player in content)
            {
                player.Online = _cacheService.GetConnectedPlayers(serverId.Value)
                    .Any(connectedPlayer => connectedPlayer.SteamID == player.SteamId64);
            }
            return new Page<PlayerDto>(content, page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task<Page<PlayerDto>> GetVipPlayers(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _playerRepository.GetVipPageByServerId(paginator, serverId!.Value, filter);
            var content = page.Content.Select(_mapper.Map<PlayerDto>);
            foreach (var player in content)
            {
                player.Online = _cacheService.GetConnectedPlayers(serverId.Value)
                    .Any(connectedPlayer => connectedPlayer.SteamID == player.SteamId64);
            }
            return new Page<PlayerDto>(content, page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public bool IsPlayerConnected(string steamId64, long? serverId)
        {
            if (serverId.HasValue)
                return _cacheService.GetConnectedPlayers(serverId.Value)
                    .Any(player => player.SteamID.Equals(steamId64));

            serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server");

            return _cacheService.GetConnectedPlayers(serverId.Value)
                .Any(player => player.SteamID.Equals(steamId64));
        }

        public List<ScumPlayer> OnlinePlayers(long serverId) => _cacheService.GetConnectedPlayers(serverId);

        public async Task<List<ScumPlayer>> OfflinePlayers(long serverId)
        {
            var allUsers = (await _playerRepository.GetAllByServerId(serverId))
                .Select(user => new ScumPlayer
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
        }

        public async Task PlayerConnected(Entities.ScumServer server, string steamId64, string scumId, string name)
        {
            var player = await _playerRepository.FindOneWithServerAsync(p => p.SteamId64 == steamId64 && p.ScumServer.Id == server.Id);

            player ??= new();
            player.SteamId64 = steamId64;
            player.ScumId = scumId;
            player.Name = name;
            player.LastLoggedIn = DateTime.Now;
            player.ScumServer = (await _scumServerRepository.FindByIdAsync(server.Id))!;

            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();


            var command = new BotCommand();
            command.ListPlayers();
            await _botService.SendCommand(server.Id, command);

        }

        public ScumPlayer? PlayerDisconnected(long serverId, string steamId64)
        {
            var player = _cacheService
                .GetConnectedPlayers(serverId)
                .FirstOrDefault(p => p.SteamID == steamId64);

            if (player is not null)
            {
                _cacheService.GetConnectedPlayers(serverId).Remove(player);
            }

            return null;
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

        public async Task<PlayerDto> AddVip(long id, PlayerVipDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (player.IsVip())
                throw new DomainException("Player already has ongoing vip");

            if (dto.DiscordRoleId.HasValue)
            {
                player.AddVip(dto.Days, dto.DiscordRoleId.Value);
                await _discordService.AddUserRoleAsync(player.ScumServer!.Guild!.DiscordId, player.DiscordId!.Value, dto.DiscordRoleId.Value);
            }
            else
            {
                player.AddVip(dto.Days);
            }

            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();

            if (dto.Whitelist)
            {
                _cacheService.EnqueueFileChangeCommand(player.ScumServerId, new Application.Models.FileChangeCommand
                {
                    Method = "vip",
                    FileChangeType = Enums.EFileChangeType.Whitelist,
                    FileChangeMethod = Enums.EFileChangeMethod.AddLine,
                    Value = player.SteamId64!,
                    ServerId = player.ScumServerId
                });
            }

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> AddSilence(long id, PlayerVipDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (player.IsSilenced())
                throw new DomainException("Player already has ongoing silence");

            player.AddSilence(dto.Days);

            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();

            _cacheService.EnqueueFileChangeCommand(player.ScumServerId, new Application.Models.FileChangeCommand
            {
                FileChangeType = Enums.EFileChangeType.SilencedUsers,
                FileChangeMethod = Enums.EFileChangeMethod.AddLine,
                Value = player.SteamId64!,
                ServerId = player.ScumServerId
            });

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> AddBan(long id, PlayerVipDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (player.IsBanned())
                throw new DomainException("Player already has ongoing ban");

            player.AddBan(dto.Days);
            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();

            _cacheService.EnqueueFileChangeCommand(player.ScumServerId, new Application.Models.FileChangeCommand
            {
                FileChangeType = Enums.EFileChangeType.BannedUsers,
                FileChangeMethod = Enums.EFileChangeMethod.AddLine,
                Value = player.SteamId64!,
                ServerId = player.ScumServerId,
                BotCommand = new BotCommand().Ban(player.SteamId64!)
            });


            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> RemoveVip(long id)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            Vip? vip = player.RemoveVip();

            if (vip is null)
                throw new DomainException("Player is not vip");

            vip.Processed = true;
            vip.Indefinitely = false;
            _unitOfWork.Vips.Update(vip);
            await _unitOfWork.SaveAsync();

            if (vip.DiscordRoleId.HasValue)
            {
                try
                {
                    await _discordService.RemoveUserRoleAsync(player.ScumServer.Guild!.DiscordId, player.DiscordId!.Value, vip.DiscordRoleId.Value);
                }
                catch (Exception)
                { }
            }

            _cacheService.EnqueueFileChangeCommand(player.ScumServerId, new Application.Models.FileChangeCommand
            {
                FileChangeType = Enums.EFileChangeType.Whitelist,
                FileChangeMethod = Enums.EFileChangeMethod.RemoveLine,
                Value = player.SteamId64!,
                ServerId = player.ScumServerId,
            });

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> RemoveSilence(long id)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            Silence? silence = player.RemoveSilence();

            if (silence is null)
                throw new DomainException("Player is not silenced");

            silence.Processed = true;
            silence.Indefinitely = false;
            _unitOfWork.Silences.Update(silence);
            await _unitOfWork.SaveAsync();

            _cacheService.EnqueueFileChangeCommand(player.ScumServerId, new Application.Models.FileChangeCommand
            {
                FileChangeType = Enums.EFileChangeType.SilencedUsers,
                FileChangeMethod = Enums.EFileChangeMethod.RemoveLine,
                Value = player.SteamId64!,
                ServerId = player.ScumServerId,
            });


            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> RemoveBan(long id)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            Ban? ban = player.RemoveBan();

            if (ban is null)
                throw new DomainException("Player is not banned");

            ban.Processed = true;
            ban.Indefinitely = false;
            _unitOfWork.Bans.Update(ban);
            await _unitOfWork.SaveAsync();

            _cacheService.EnqueueFileChangeCommand(player.ScumServerId, new Application.Models.FileChangeCommand
            {
                FileChangeType = Enums.EFileChangeType.BannedUsers,
                FileChangeMethod = Enums.EFileChangeMethod.RemoveLine,
                Value = player.SteamId64!,
                ServerId = player.ScumServerId,
            });

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> RemoveDiscordRole(long id, ulong roleId)
        {
            var player = await _playerRepository.FindByIdAsync(id);
            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            var role = player.DiscordRoles?.FirstOrDefault(role => role.DiscordId == roleId);

            if (role is null)
                throw new DomainException("Player is not banned");

            role.Processed = true;
            _unitOfWork.DiscordRoles.Update(role);
            await _unitOfWork.SaveAsync();

            try
            {
                await _discordService.RemoveUserRoleAsync(player.ScumServer.Guild.DiscordId, player.DiscordId.Value, role.DiscordId);
            }
            catch (Exception) { }

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> AddDiscordRole(long id, PlayerVipDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);

            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (!dto.DiscordRoleId.HasValue)
                throw new NotFoundException("Invalid role");

            if (player.DiscordRoles is null || player.DiscordRoles.Any(role => role.DiscordId == dto.DiscordRoleId))
                throw new DomainException("Player already has this role");

            await _discordService.AddUserRoleAsync(player.ScumServer!.Guild!.DiscordId, player.DiscordId!.Value, dto.DiscordRoleId.Value);
            player.AddDiscordRole(dto.Days, dto.DiscordRoleId.Value);

            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();

            return _mapper.Map<PlayerDto>(player);

        }

        public async Task<PlayerDto> UpdateCoins(long id, ChangeAmountDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);

            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (dto.Amount > 0)
            {
                await _unitOfWork.AppDbContext.Database.ExecuteSqlRawAsync("SELECT addcoinstoplayer({0}, {1})", player.Id, dto.Amount);
            }
            else
            {
                await _unitOfWork.AppDbContext.Database.ExecuteSqlRawAsync("SELECT reducecoinstoplayer({0}, {1})", player.Id, Math.Abs(dto.Amount));
            }

            player.Coin += dto.Amount;

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> UpdateFame(long id, ChangeAmountDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);

            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (!_botService.IsBotOnline(player.ScumServerId))
                throw new DomainException("There is no bots online at the moment");

            await _botService.SendCommand(player.ScumServerId, new BotCommand().ChangeFame(player.SteamId64!, dto.Amount));

            player.Fame += dto.Amount;

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> UpdateGold(long id, ChangeAmountDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);

            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (!_botService.IsBotOnline(player.ScumServerId))
                throw new DomainException("There is no bots online at the moment");

            await _botService.SendCommand(player.ScumServerId, new BotCommand().ChangeGold(player.SteamId64!, dto.Amount));

            player.Fame += dto.Amount;

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto> UpdateMoney(long id, ChangeAmountDto dto)
        {
            var player = await _playerRepository.FindByIdAsync(id);

            if (player is null)
                throw new NotFoundException("Player not found");

            ValidateServerOwner(player.ScumServer);
            ValidateSubscription(player.ScumServer);

            if (!_botService.IsBotOnline(player.ScumServerId))
                throw new DomainException("There is no bots online at the moment");

            await _botService.SendCommand(player.ScumServerId, new BotCommand().ChangeMoney(player.SteamId64!, dto.Amount));

            player.Fame += dto.Amount;

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<List<LockpickStatsDto>> LockpickRank()
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            var lockpicks = _unitOfWork.Lockpicks
                .Include(kill => kill.ScumServer)
                .Where(l => l.ScumServer.Id == server.Id);

            return await lockpicks
                .GroupBy(l => new { l.Name, l.LockType })
                .Select(g => new LockpickStatsDto
                {
                    PlayerName = g.Key.Name,
                    LockType = g.Key.LockType,
                    SuccessCount = g.Count(l => l.Success),
                    FailCount = g.Count(l => !l.Success),
                    SuccessRate = g.Any()
                        ? (double)g.Count(l => l.Success) / g.Count() * 100
                        : 0
                })
                .OrderByDescending(p => p.SuccessRate)
                .ThenByDescending(p => p.SuccessCount)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<LockpickStatsDto>> LockpickRank(string steamId)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            var lockpicks = _unitOfWork.Lockpicks
                .Include(kill => kill.ScumServer)
                .Where(l => l.ScumServer.Id == server.Id && l.SteamId64 == steamId);

            return await lockpicks
                .GroupBy(l => new { l.Name, l.LockType })
                .Select(g => new LockpickStatsDto
                {
                    PlayerName = g.Key.Name,
                    LockType = g.Key.LockType,
                    SuccessCount = g.Count(l => l.Success),
                    FailCount = g.Count(l => !l.Success),
                    SuccessRate = g.Any()
                        ? (double)g.Count(l => l.Success) / g.Count() * 100
                        : 0
                })
                .OrderByDescending(p => p.SuccessRate)
                .ThenByDescending(p => p.SuccessCount)
                .Take(20)
                .ToListAsync();
        }


        public async Task<List<PlayerStatsDto>> KillRank()
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            // Filter kills by server and period
            var kills = _unitOfWork.Kills
                .Include(k => k.ScumServer)
                .Where(k => k.ScumServer.Id == server.Id && k.KillerSteamId64 != "-1");

            // Group by KillerName
            return await kills
                .GroupBy(k => k.KillerName)
                .Select(g => new PlayerStatsDto()
                {
                    PlayerName = g.Key!,
                    SteamId = g.FirstOrDefault(x => x.KillerName == g.Key)!.KillerSteamId64!,
                    KillCount = g.Count()
                })
                .OrderByDescending(k => k.KillCount)
                .ToListAsync();
        }

        public async Task<List<PlayerStatsDto>> KillRank(string steamId)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            // Filter kills by server and period
            var kills = _unitOfWork.Kills
                .Include(k => k.ScumServer)
                .Where(k => k.ScumServer.Id == server.Id && k.KillerSteamId64 == steamId);

            // Group by KillerName
            return await kills
                .GroupBy(k => k.KillerName)
                .Select(g => new PlayerStatsDto()
                {
                    PlayerName = g.Key!,
                    KillCount = g.Count()
                })
                .OrderByDescending(k => k.KillCount)
                .ToListAsync();
        }

        public async Task<List<GrapthDto>> NewPlayersPerMonth()
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            var monthlyCounts = _unitOfWork.Players
              .Include(player => player.ScumServer)
              .Where(player => player.CreateDate.Year == DateTime.UtcNow.Year && player.ScumServerId == serverId.Value)
              .GroupBy(p => new { p.CreateDate.Year, p.CreateDate.Month })
              .Select(g => new
              {
                  Year = g.Key.Year,
                  Month = g.Key.Month,
                  Count = g.Count()
              })
              .OrderBy(x => x.Year)
              .ThenBy(x => x.Month)
              .ToList();

            return monthlyCounts
                .Select(x => new GrapthDto
                {
                    Amount = x.Count, // players in that month only
                    Color = ColorUtil.GetRandomColor(),
                    Name = new DateTime(x.Year, x.Month, 1).ToString("MMMM", CultureInfo.InvariantCulture)
                })
                .ToList();
        }

        public Task<int> GetCount() => _playerRepository.GetCount(ServerId()!.Value);
        public Task<int> GetVipCount() => _playerRepository.GetVipCount(ServerId()!.Value);

        public async Task<List<string>> GetServerWhitelistValue(Ftp ftp)
        {
            List<string> steamIds = [];
            if (ftp is null) throw new DomainException("Invalid ftp server");
            try
            {
                var client = await _ftpService.GetClientAsync(ftp);
                using (var stream = await client.OpenRead($@"{ftp.RootFolder}/Saved/Config/WindowsServer/WhitelistedUsers.ini"))
                using (var reader = new StreamReader(stream, encoding: Encoding.UTF8))
                    while (await reader.ReadLineAsync() is { } line)
                        if (!string.IsNullOrEmpty(line)) steamIds.Add(line);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new DomainException("Invalid FTP server");
            }
            return steamIds;
        }

        public async Task<int> GetWhitelistCount()
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            var steamIds = await GetServerWhitelistValue(server.Ftp!);
            return steamIds.Count();
        }
    }
}
