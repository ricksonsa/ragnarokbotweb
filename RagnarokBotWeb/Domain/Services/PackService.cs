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
    public class PackService : BaseService, IPackService
    {
        private readonly ILogger<PackService> _logger;

        private readonly IPackRepository _packRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IPackItemRepository _packItemRepository;
        private readonly IDiscordService _discordService;
        private readonly IMapper _mapper;

        public PackService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<PackService> logger,
            IPackRepository packRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IDiscordService discordService,
            IPackItemRepository packItemRepository) : base(httpContextAccessor)
        {
            _logger = logger;
            _packRepository = packRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _discordService = discordService;
            _packItemRepository = packItemRepository;
        }

        public async Task<PackDto> CreatePackAsync(PackDto createPack)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            var pack = _mapper.Map<Pack>(createPack);
            pack.ScumServer = server;
            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            if (!string.IsNullOrEmpty(pack.DiscordChannelId))
            {
                pack.DiscordMessageId = await GenerateDiscordPackButton(pack);
                await _packRepository.CreateOrUpdateAsync(pack);
                await _packRepository.SaveAsync();
            }

            return _mapper.Map<PackDto>(pack);
        }

        private async Task<ulong> GenerateDiscordPackButton(Pack pack)
        {
            var action = $"buy_package:{pack.Id}";
            var embed = new CreateEmbed
            {
                Buttons = [new($"Buy {pack.Name}", action)],
                DiscordId = ulong.Parse(pack.DiscordChannelId!),
                Text = pack.Description,
                ImageUrl = pack.ImageUrl,
                Title = pack.Name
            };
            IUserMessage message;
            if (embed.ImageUrl != null)
                message = await _discordService.SendEmbedWithBase64Image(embed);
            else
                message = await _discordService.SendEmbedToChannel(embed);

            return message.Id;
        }

        private async Task DeleteDiscordMessage(Pack? pack)
        {
            if (pack?.DiscordChannelId != null && pack.DiscordMessageId != null)
            {
                await _discordService.RemoveMessage(ulong.Parse(pack.DiscordChannelId!), pack.DiscordMessageId!.Value);
            }
        }

        public async Task<PackDto> FetchPackById(long id)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new UnauthorizedException("Invalid server");

            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null || pack.Deleted != null) throw new NotFoundException("Package not found");

            if (pack.ScumServer.Id != server.Id) throw new UnauthorizedException("Invalid package");

            return _mapper.Map<PackDto>(pack);
        }

        public async Task<PackDto> UpdatePackAsync(long id, PackDto packDto)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var packNotTracked = await _packRepository.FindByIdAsNoTrackingAsync(id);
            if (packNotTracked is null) throw new NotFoundException("Pack not found");

            if (packNotTracked.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            _packItemRepository.DeletePackItems(packNotTracked.PackItems);
            await _packItemRepository.SaveAsync();

            var pack = _mapper.Map<Pack>(packDto);
            pack.ScumServer = packNotTracked.ScumServer;

            if (!string.IsNullOrEmpty(packDto.DiscordChannelId) && packDto.DiscordChannelId != packNotTracked.DiscordChannelId)
            {
                if (packNotTracked.DiscordMessageId != null)
                {
                    await _discordService.RemoveMessage(ulong.Parse(packNotTracked.DiscordChannelId!), packNotTracked.DiscordMessageId!.Value);
                }

                pack.DiscordChannelId = packDto.DiscordChannelId;
                pack.DiscordMessageId = await GenerateDiscordPackButton(pack);

            }

            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            return await FetchPackById(id);
        }

        public async Task DeletePackAsync(long id)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null) throw new NotFoundException("Package not found");

            if (pack.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            _packItemRepository.DeletePackItems(pack.PackItems);
            await _packItemRepository.SaveAsync();

            await DeleteDiscordMessage(pack);

            pack.Deleted = DateTime.UtcNow;
            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            return;
        }

        public async Task<Page<PackDto>> GetPacksPageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var page = await _packRepository.GetPageByServerAndFilter(paginator, serverId.Value, filter);
            return new Page<PackDto>(page.Content.Select(_mapper.Map<PackDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task<PackDto> FetchWelcomePack()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var pack = await _packRepository.FindOneAsync(package => package.IsWelcomePack);
            if (pack == null) throw new NotFoundException("Package not found");

            return await FetchPackById(pack.Id);
        }
    }
}
