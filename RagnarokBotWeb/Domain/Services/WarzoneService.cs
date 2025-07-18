using AutoMapper;
using Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscordService _discordService;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IMapper _mapper;
        private readonly ITaskService _taskService;

        public WarzoneService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<WarzoneService> logger,
            IWarzoneRepository warzoneRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            IDiscordService discordService,
            ITaskService taskService) : base(httpContextAccessor)
        {
            _logger = logger;
            _warzoneRepository = warzoneRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
            _discordService = discordService;
            _taskService = taskService;
        }

        public async Task<WarzoneDto> CreateWarzoneAsync(WarzoneDto createWarzone)
        {
            var serverId = ServerId();
            var warzone = _mapper.Map<Warzone>(createWarzone);

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            warzone.ScumServer = server;

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
                Text = warzone.Description,
                ImageUrl = warzone.ImageUrl,
                Title = warzone.Name
            };
            IUserMessage message;
            if (embed.ImageUrl != null)
                message = await _discordService.SendEmbedWithBase64Image(embed);
            else
                message = await _discordService.SendEmbedToChannel(embed);

            return message.Id;
        }

        public async Task DeleteDiscordMessage(Warzone? warzone)
        {
            await _discordService.RemoveMessage(ulong.Parse(warzone.DiscordChannelId!), warzone.DiscordMessageId!.Value);
        }

        public async Task<WarzoneDto> UpdateWarzoneAsync(long id, WarzoneDto warzoneDto)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var warzoneNotTracked = await _warzoneRepository.FindByIdAsNoTrackingAsync(id);
            if (warzoneNotTracked is null) throw new NotFoundException("Pack not found");

            if (warzoneNotTracked.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            if (warzoneNotTracked.WarzoneItems.Any())
            {
                warzoneNotTracked.WarzoneItems.ForEach(wi => wi.Item = null);
                _unitOfWork.WarzoneItems.RemoveRange(warzoneNotTracked.WarzoneItems);
            }

            if (warzoneNotTracked.Teleports.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.Teleports.Select(x => x.Teleport));
                _unitOfWork.WarzoneTeleports.RemoveRange(warzoneNotTracked.Teleports);
            }

            if (warzoneNotTracked.SpawnPoints.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.SpawnPoints.Select(x => x.Teleport));
                _unitOfWork.WarzoneSpawns.RemoveRange(warzoneNotTracked.SpawnPoints);
            }

            var warzone = _mapper.Map<Warzone>(warzoneDto);
            warzone.ScumServer = warzoneNotTracked.ScumServer;

            if (!string.IsNullOrEmpty(warzoneDto.DiscordChannelId) && warzoneDto.DiscordChannelId != warzoneNotTracked.DiscordChannelId)
            {
                if (warzoneNotTracked.DiscordMessageId != null)
                {
                    await _discordService.RemoveMessage(ulong.Parse(warzoneNotTracked.DiscordChannelId!), warzoneNotTracked.DiscordMessageId!.Value);
                }

                warzone.DiscordChannelId = warzoneDto.DiscordChannelId;

                if (warzoneNotTracked.IsRunning)
                {
                    warzone.DiscordMessageId = await GenerateDiscordWarzoneButton(warzone);
                }
            }

            await _warzoneRepository.CreateOrUpdateAsync(warzone);
            await _warzoneRepository.SaveAsync();

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task DeleteWarzoneAsync(long id)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var warzoneNotTracked = await _warzoneRepository.FindByIdAsNoTrackingAsync(id);
            if (warzoneNotTracked is null) throw new NotFoundException("Package not found");
            if (warzoneNotTracked.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            if (warzoneNotTracked.WarzoneItems.Any())
            {
                warzoneNotTracked.WarzoneItems.ForEach(wi => wi.Item = null);
                _unitOfWork.WarzoneItems.RemoveRange(warzoneNotTracked.WarzoneItems);
            }

            if (warzoneNotTracked.Teleports.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.Teleports.Select(x => x.Teleport));
                _unitOfWork.WarzoneTeleports.RemoveRange(warzoneNotTracked.Teleports);
            }

            if (warzoneNotTracked.SpawnPoints.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.SpawnPoints.Select(x => x.Teleport));
                _unitOfWork.WarzoneSpawns.RemoveRange(warzoneNotTracked.SpawnPoints);
            }
            await _warzoneRepository.SaveAsync();

            var warzone = await _warzoneRepository.FindByIdAsync(id);
            warzone!.Deleted = DateTime.UtcNow;
            await _warzoneRepository.CreateOrUpdateAsync(warzoneNotTracked);
            await _warzoneRepository.SaveAsync();

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
            var page = await _warzoneRepository.GetPageByServerAndFilter(paginator, serverId.Value, filter);
            return new Page<WarzoneDto>(page.Content.Select(_mapper.Map<WarzoneDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }


    }
}
