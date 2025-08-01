using AutoMapper;
using Discord;
using Microsoft.EntityFrameworkCore;
using Quartz;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Tasks.Jobs;
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

            warzone.ScumServer = server;

            if (!string.IsNullOrEmpty(warzone.ImageUrl))
            {
                warzone.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(warzone.ImageUrl);
            }

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
                DiscordId = ulong.Parse(warzone.DiscordChannelId!),
                FooterText = GetFooterText(warzone),
                Color = warzone.IsVipOnly ? Color.Gold : Color.Blue,
                Text = warzone.Description,
                ImageUrl = warzone.ImageUrl,
                Title = warzone.Name
            };

            IUserMessage message = await _discordService.SendEmbedToChannel(embed);
            return message.Id;
        }

        private static string GetFooterText(Warzone warzone)
        {
            string text = string.Empty;

            if (warzone.Price > 0) text = $"Price: {warzone.Price}";
            if (warzone.VipPrice > 0) text += $"\nVip price: {warzone.VipPrice}";
            if (warzone.IsVipOnly) text = $"Price: {warzone.VipPrice}";

            return text;
        }

        public async Task DeleteDiscordMessage(Warzone? warzone)
        {
            await _discordService.RemoveMessage(ulong.Parse(warzone.DiscordChannelId!), warzone.DiscordMessageId!.Value);
        }

        public async Task<WarzoneDto> UpdateWarzoneAsync(long id, WarzoneDto warzoneDto)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var warzone = await _warzoneRepository.FindByIdAsync(id);

            if (warzone == null)
                throw new NotFoundException("Warzone not found");


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

            if (warzone.IsRunning && !warzoneDto.Enabled)
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
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");
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

            // === Remove related entities ===

            _unitOfWork.AppDbContext.WarzoneItems.RemoveRange(warzone.WarzoneItems);
            _unitOfWork.AppDbContext.WarzoneSpawns.RemoveRange(warzone.SpawnPoints);
            _unitOfWork.AppDbContext.Teleports.RemoveRange(warzone.SpawnPoints.Select(sp => sp.Teleport));

            // === Remove main entity ===

            _unitOfWork.AppDbContext.Warzones.Remove(warzone);
            await _unitOfWork.AppDbContext.SaveChangesAsync();

            try
            {
                await _taskService.DeleteJob($"WarzoneItemSpawnJob({serverId.Value})");
            }
            catch (Exception) { }

            try
            {
                await _taskService.DeleteJob($"CloseWarzoneJob({serverId.Value})");
            }
            catch (Exception) { }

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

            if (warzone.ScumServer.Id != serverId!.Value) throw new UnauthorizedException("Invalid server id");

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task<Page<WarzoneDto>> GetWarzonePageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _warzoneRepository.GetPageByServerAndFilter(paginator, serverId!.Value, filter);
            return new Page<WarzoneDto>(page.Content.Select(_mapper.Map<WarzoneDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task OpenWarzone(ScumServer server, CancellationToken token = default)
        {
            var warzones = await _unitOfWork.Warzones
               .Include(warzone => warzone.ScumServer)
               .Where(warzone => warzone.Enabled && warzone.ScumServer.Id == server.Id)
               .ToListAsync();

            var warzone = new WarzoneSelector(_cacheService, server).Select(warzones);

            if (warzone is null) return;

            if (!warzone.IsRunning)
            {
                warzone.Run();
                _unitOfWork.Warzones.Update(warzone);
                await _unitOfWork.SaveAsync();
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            if (await scheduler.CheckExists(new JobKey($"CloseWarzoneJob({server.Id})"), token))
            {
                await CloseWarzone(server, token);
            }

            var closeWarzoneJob = JobBuilder.Create<CloseWarzoneJob>()
                  .WithIdentity($"CloseWarzoneJob({server.Id})")
                  .UsingJobData("server_id", server.Id)
                  .UsingJobData("warzone_id", warzone.Id)
                  .Build();

            ITrigger warzoneClosingTrigger = TriggerBuilder.Create()
                .WithIdentity("CloseWarzoneJobTrigger", server.Id.ToString())
                .StartAt(new DateTimeOffset(warzone.StopAt!.Value))
                .WithSimpleSchedule(x => x.WithRepeatCount(0)) // only once
                .Build();


            await scheduler.ScheduleJob(closeWarzoneJob, warzoneClosingTrigger);

            if (!string.IsNullOrEmpty(warzone.StartMessage))
            {
                var command = new BotCommand();
                command.Announce(warzone.StartMessage);
                _cacheService.GetCommandQueue(server.Id).Enqueue(command);
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
                    CreateEmbed embed = warzone.WarzoneButtonEmbed();
                    IUserMessage message;
                    if (embed.ImageUrl != null)
                        message = await _discordService.SendEmbedWithBase64Image(embed);
                    else
                        message = await _discordService.SendEmbedToChannel(embed);

                    warzone.DiscordMessageId = message.Id;

                }
                catch (Exception) { }

                _unitOfWork.Warzones.Update(warzone);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Warzone Id {Warzone} Opened for Server Id {Server} at: {time}", warzone.Id, server.Id, DateTimeOffset.Now);

            }

            var warzoneItemSpawnJob = JobBuilder.Create<WarzoneItemSpawnJob>()
                  .WithIdentity($"WarzoneItemSpawnJob({server.Id})")
                  .UsingJobData("server_id", server.Id)
                  .UsingJobData("warzone_id", warzone.Id)
                  .Build();

            ITrigger warzoneItemSpawnJobTrigger = TriggerBuilder.Create()
               .WithIdentity("WarzoneItemSpawnJobTrigger", server.Id.ToString())
               .StartNow() // Start immediately
               .WithSimpleSchedule(x => x
                   .WithIntervalInMinutes((int)warzone.ItemSpawnInterval)
                   .RepeatForever())
               .Build();

            await scheduler.ScheduleJob(warzoneItemSpawnJob, warzoneItemSpawnJobTrigger);
        }

        public async Task CloseWarzone(ScumServer server, CancellationToken token = default)
        {
            var warzone = await _unitOfWork.Warzones
                .Include(warzone => warzone.ScumServer)
                .FirstOrDefaultAsync(warzone => warzone.ScumServer.Id == server.Id && warzone.StopAt.HasValue && DateTime.Now < warzone.StopAt.Value, cancellationToken: token);

            if (warzone == null) return;

            warzone.Stop();
            _unitOfWork.Warzones.Update(warzone);
            await _unitOfWork.SaveAsync();

            var scheduler = await _schedulerFactory.GetScheduler(token);
            JobKey warzoneItemSpawnJobKey = new($"WarzoneItemSpawnJob({server.Id})");
            JobKey closeWarzoneJobKey = new($"CloseWarzoneJob({server.Id})");

            try
            {
                await scheduler.DeleteJob(warzoneItemSpawnJobKey, token);
            }
            catch (Exception)
            {
                _logger.LogWarning("Tried delete inexisting jobs {Job}", closeWarzoneJobKey.Name);
            }

            try
            {
                await scheduler.DeleteJob(closeWarzoneJobKey, token);
            }
            catch (Exception)
            {

                _logger.LogWarning("Tried delete inexisting jobs {Job}", warzoneItemSpawnJobKey.Name);
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
        }
    }
}
