using AutoMapper;
using Discord;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class WarzoneService : BaseService, IWarzoneService
    {
        private readonly ILogger<WarzoneService> _logger;
        private readonly IWarzoneRepository _warzoneRepository;
        private readonly IFileService _fileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscordService _discordService;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IMapper _mapper;
        private readonly ITaskService _taskService;
        private readonly ICacheService _cacheService;

        public WarzoneService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<WarzoneService> logger,
            IWarzoneRepository warzoneRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            IDiscordService discordService,
            ITaskService taskService,
            ICacheService cacheService,
            ISchedulerFactory schedulerFactory,
            IFileService fileService) : base(httpContextAccessor)
        {
            _logger = logger;
            _warzoneRepository = warzoneRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
            _discordService = discordService;
            _taskService = taskService;
            _cacheService = cacheService;
            _schedulerFactory = schedulerFactory;
            _fileService = fileService;
        }

        public async Task<WarzoneDto> CreateWarzoneAsync(WarzoneDto createWarzone)
        {
            var serverId = ServerId();
            var warzone = _mapper.Map<Warzone>(createWarzone);

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            ValidateSubscription(server);

            warzone.ScumServer = server;
            warzone.WarzoneItems = createWarzone.WarzoneItems.Select(_mapper.Map<WarzoneItem>).ToList();
            warzone.Teleports = createWarzone.Teleports.Select(_mapper.Map<WarzoneTeleport>).ToList();
            warzone.SpawnPoints = createWarzone.SpawnPoints.Select(_mapper.Map<WarzoneSpawn>).ToList();

            if (!string.IsNullOrEmpty(warzone.ImageUrl))
                warzone.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(warzone.ImageUrl);

            await _warzoneRepository.CreateOrUpdateAsync(warzone);
            await _warzoneRepository.SaveAsync();

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task<ulong> GenerateDiscordWarzoneButton(Warzone warzone)
        {
            var action = $"buy_warzone:{warzone.Id}";
            var embed = new CreateEmbed
            {
                Buttons = [new($"Buy {warzone.Name} Teleport", action)],
                GuildId = warzone.ScumServer!.Guild!.DiscordId,
                DiscordId = ulong.Parse(warzone.DiscordChannelId!),
                Fields = GetFields(warzone),
                Color = warzone.IsVipOnly ? Color.Gold : Color.DarkOrange,
                Text = warzone.Description,
                ImageUrl = warzone.ImageUrl,
                Title = warzone.Name
            };

            IUserMessage message = await _discordService.SendEmbedToChannel(embed);
            return message.Id;
        }

        private static List<CreateEmbedField> GetFields(Warzone warzone)
        {
            List<CreateEmbedField> fields = [];
            if (warzone.Price > 0) fields.Add(new CreateEmbedField("Price", warzone.Price.ToString(), true));
            if (warzone.VipPrice > 0) fields.Add(new CreateEmbedField("Vip Price", warzone.VipPrice.ToString(), true));
            return fields;
        }

        public async Task DeleteDiscordMessage(Warzone warzone)
        {
            await _discordService.RemoveMessage(ulong.Parse(warzone.DiscordChannelId!), warzone.DiscordMessageId!.Value);
        }

        public async Task<WarzoneDto> UpdateWarzoneAsync(long id, WarzoneDto warzoneDto)
        {
            var serverId = ServerId();
            var warzone = await _warzoneRepository.FindByIdAsync(id);

            if (warzone == null)
                throw new NotFoundException("Warzone not found");


            ValidateServerOwner(warzone.ScumServer);
            ValidateSubscription(warzone.ScumServer);

            var previousImage = warzone.ImageUrl;
            var previousDiscordId = warzone.DiscordChannelId;
            _mapper.Map(warzoneDto, warzone);

            if (!string.IsNullOrEmpty(warzone.ImageUrl) && warzone.ImageUrl != previousImage)
            {
                if (!string.IsNullOrEmpty(previousImage)) _fileService.DeleteFile(previousImage);
                warzone.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(warzone.ImageUrl);
            }
            RemoveWarzoneItems(warzoneDto, warzone);
            RemoveWarzoneSpawnPoints(warzoneDto, warzone);
            RemoveWarzoneTeleports(warzoneDto, warzone);

            var scheduler = await _schedulerFactory.GetScheduler();
            if (await scheduler.CheckExists(new JobKey($"CloseWarzoneJob({serverId.Value})")) && !warzoneDto.Enabled)
            {
                await CloseWarzone(warzone.ScumServer);
            }

            if (warzone.Enabled)
            {
                try
                {
                    await _discordService.RemoveMessage(ulong.Parse(previousDiscordId!), warzone.DiscordMessageId!.Value);
                }
                catch (Exception)
                { }

                if (warzone.IsRunning)
                {
                    warzone.DiscordMessageId = await GenerateDiscordWarzoneButton(warzone);
                }
            }
            else if (!string.IsNullOrEmpty(warzoneDto.DiscordChannelId) && warzoneDto.DiscordChannelId != previousDiscordId)
            {
                try
                {
                    await _discordService.RemoveMessage(ulong.Parse(previousDiscordId!), warzone.DiscordMessageId!.Value);
                }
                catch (Exception)
                { }
            }

            _unitOfWork.Warzones.Update(warzone);
            await _unitOfWork.SaveAsync();

            return _mapper.Map<WarzoneDto>(warzone);
        }

        private void RemoveWarzoneItems(WarzoneDto warzoneDto, Warzone existingWarzone)
        {
            var updatedItemIds = warzoneDto.WarzoneItems.Select(wi => wi.ItemId).ToHashSet();

            // Remove WarzoneItems no longer present
            foreach (var existingItem in existingWarzone.WarzoneItems.ToList())
            {
                if (!updatedItemIds.Contains(existingItem.ItemId))
                {
                    _unitOfWork.AppDbContext.WarzoneItems.Remove(existingItem);
                }
            }

            // Add new WarzoneItems
            foreach (var updatedItem in warzoneDto.WarzoneItems)
            {
                if (!existingWarzone.WarzoneItems.Any(wi => wi.ItemId == updatedItem.ItemId))
                {
                    existingWarzone.WarzoneItems.Add(new WarzoneItem
                    {
                        ItemId = updatedItem.ItemId,
                        WarzoneId = existingWarzone.Id
                    });
                }
            }
        }

        private void RemoveWarzoneTeleports(WarzoneDto warzoneDto, Warzone existingWarzone)
        {
            var updatedItemIds = warzoneDto.Teleports.Select(wi => wi.TeleportId).ToHashSet();

            // Remove WarzoneItems no longer present
            foreach (var existingItem in existingWarzone.Teleports.ToList())
            {
                if (!updatedItemIds.Contains(existingItem.TeleportId))
                {
                    _unitOfWork.AppDbContext.WarzoneTeleports.Remove(existingItem);
                }
            }

            // Add new WarzoneItems
            foreach (var dto in warzoneDto.Teleports)
            {
                if (!existingWarzone.Teleports.Any(wi => wi.TeleportId == dto.TeleportId))
                {
                    existingWarzone.Teleports.Add(new WarzoneTeleport
                    {
                        Teleport = _mapper.Map<Teleport>(dto.Teleport),
                        WarzoneId = existingWarzone.Id
                    });
                }
            }
        }

        private void RemoveWarzoneSpawnPoints(WarzoneDto warzoneDto, Warzone existingWarzone)
        {
            var updatedItemIds = warzoneDto.SpawnPoints.Select(wi => wi.TeleportId).ToHashSet();

            // Remove WarzoneItems no longer present
            foreach (var existingItem in existingWarzone.SpawnPoints.ToList())
            {
                if (!updatedItemIds.Contains(existingItem.TeleportId))
                {
                    _unitOfWork.AppDbContext.WarzoneSpawns.Remove(existingItem);
                }
            }

            // Add new WarzoneItems
            foreach (var dto in warzoneDto.SpawnPoints)
            {
                if (!existingWarzone.SpawnPoints.Any(wi => wi.TeleportId == dto.TeleportId))
                {
                    existingWarzone.SpawnPoints.Add(new WarzoneSpawn
                    {
                        Teleport = _mapper.Map<Teleport>(dto.Teleport),
                        WarzoneId = existingWarzone.Id
                    });
                }
            }
        }

        public async Task DeleteWarzoneAsync(long id)
        {
            var serverId = ServerId();
            await CheckAuthority(id, serverId);

            var warzone = await _unitOfWork.AppDbContext.Warzones
              .Include(w => w.WarzoneItems)
              .Include(w => w.SpawnPoints)
                  .ThenInclude(sp => sp.Teleport)
              .Include(w => w.Teleports)
                  .ThenInclude(w => w.Teleport)
              .FirstOrDefaultAsync(w => w.Id == id);

            if (warzone == null)
                throw new NotFoundException("Warzone not found");

            // Optionally clean up Discord message
            if (!string.IsNullOrEmpty(warzone.DiscordChannelId) && warzone.DiscordMessageId.HasValue)
            {
                try
                {
                    await _discordService.RemoveMessage(
                        ulong.Parse(warzone.DiscordChannelId),
                        warzone.DiscordMessageId.Value
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to remove Discord message: {0}", ex.Message);
                }
            }

            if (warzone.IsRunning)
            {
                await CloseWarzone(warzone.ScumServer);
            }

            // === Remove related entities ===

            _unitOfWork.AppDbContext.WarzoneItems.RemoveRange(warzone.WarzoneItems);
            _unitOfWork.AppDbContext.WarzoneSpawns.RemoveRange(warzone.SpawnPoints);
            _unitOfWork.AppDbContext.Teleports.RemoveRange(warzone.SpawnPoints.Select(sp => sp.Teleport));

            // === Remove main entity ===

            _unitOfWork.AppDbContext.Warzones.Remove(warzone);
            await _unitOfWork.AppDbContext.SaveChangesAsync();

            return;
        }

        private async Task CheckAuthority(long id, long? serverId)
        {
            if (!await _unitOfWork.AppDbContext.Warzones.AsNoTracking().Include(wz => wz.ScumServer).AnyAsync(wz => wz.ScumServer.Id == serverId && wz.Id == id))
            {
                throw new UnauthorizedException("Invalid warzone");
            }
        }

        public async Task<WarzoneDto> FetchWarzoneById(long id)
        {
            var serverId = ServerId();
            var warzone = await _warzoneRepository.FindByIdAsync(id);
            if (warzone is null) throw new NotFoundException("Warzone not found");

            ValidateServerOwner(warzone.ScumServer);

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task<Page<WarzoneDto>> GetWarzonePageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _warzoneRepository.GetPageByServerAndFilter(paginator, serverId!.Value, filter);
            return new Page<WarzoneDto>(page.Content.Select(_mapper.Map<WarzoneDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task<WarzoneDto?> OpenWarzone(bool? force = false)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindByIdAsync(serverId!.Value);
            if (server is null) throw new NotFoundException("Server not found");

            ValidateSubscription(server);

            return await OpenWarzone(server, force);
        }

        public async Task<WarzoneDto?> CloseWarzone()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");
            var server = await _scumServerRepository.FindByIdAsync(serverId!.Value);
            if (server is null) throw new NotFoundException("Server not found");
            return await CloseWarzone(server);
        }

        public async Task<WarzoneDto?> OpenWarzone(ScumServer server, bool? force = false, CancellationToken token = default)
        {
            var warzones = await _unitOfWork.Warzones
               .Include(warzone => warzone.ScumServer)
               .Include(warzone => warzone.ScumServer.Guild)
               .Where(warzone => warzone.ScumServer.Id == server.Id)
               .ToListAsync();

            var warzone = new WarzoneSelector(_cacheService, server).Select(warzones, force);

            if (warzone is null) return null;

            if (!warzone.IsRunning)
            {
                warzone.Run();
                _unitOfWork.Warzones.Update(warzone);
                await _unitOfWork.SaveAsync();
            }

            var scheduler = await _schedulerFactory.GetScheduler();
            if (await scheduler.CheckExists(new JobKey($"CloseWarzoneJob({server.Id})"), token))
                await CloseWarzone(server, token);

            if (!string.IsNullOrEmpty(warzone.StartMessage))
            {
                _cacheService.GetCommandQueue(server.Id).Enqueue(new BotCommand().Announce(warzone.StartMessage));
            }

            if (warzone.DiscordChannelId != null)
            {
                try
                {
                    await _discordService.RemoveMessage(ulong.Parse(warzone.DiscordChannelId!), warzone.DiscordMessageId!.Value);
                }
                catch (Exception) { }

                try
                {
                    warzone.DiscordMessageId = await GenerateDiscordWarzoneButton(warzone);
                }
                catch (Exception) { }

                _unitOfWork.Warzones.Update(warzone);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Warzone Id {Warzone} Opened for Server Id {Server} at: {time}", warzone.Id, server.Id, DateTimeOffset.Now);

            }

            await _taskService.CreateWarzoneJobs(server, warzone);
            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task<WarzoneDto?> CloseWarzone(ScumServer server, CancellationToken token = default)
        {
            var warzones = await _unitOfWork.Warzones
                .Include(warzone => warzone.ScumServer)
                .ToListAsync();

            var warzone = warzones.FirstOrDefault(wz => wz.IsRunning);

            if (warzone == null) return null;

            warzone.Stop();
            _unitOfWork.Warzones.Update(warzone);
            await _unitOfWork.SaveAsync();

            try
            {
                await _taskService.DeleteWarzoneJobs(server);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete warzone jobs for server {Server} -> {Ex}", server.Id, ex.Message);
            }

            if (warzone.DiscordChannelId != null)
            {
                try
                {
                    await _discordService.RemoveMessage(ulong.Parse(warzone.DiscordChannelId!), warzone.DiscordMessageId!.Value);
                }
                catch (Exception) { }
            }

            _logger.LogInformation("Warzone Id {Warzone} Closed for Server Id {Id} at: {time}", warzone.Id, server.Id, DateTimeOffset.Now);

            return _mapper.Map<WarzoneDto>(warzone);
        }
    }
}
