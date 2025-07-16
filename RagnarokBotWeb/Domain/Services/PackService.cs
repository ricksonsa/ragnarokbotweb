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
        private readonly IPackItemRepository _packItemRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IDiscordService _discordService;
        private readonly IMapper _mapper;

        public PackService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<PackService> logger,
            IPackRepository packRepository,
            IPackItemRepository packItemRepository,
            IItemRepository itemRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IDiscordService discordService) : base(httpContextAccessor)
        {
            _logger = logger;
            _packRepository = packRepository;
            _packItemRepository = packItemRepository;
            _itemRepository = itemRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _discordService = discordService;
        }

        public async Task<PackDto> CreatePackAsync(PackDto createPack)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            var pack = _mapper.Map<Pack>(createPack);
            pack.ScumServer = server;
            pack.PackItems = null;
            await _packRepository.AddAsync(pack);
            await _packRepository.SaveAsync();

            foreach (var item in createPack.Items)
            {
                var packItem = new PackItem
                {
                    Amount = item.Amount,
                    Item = await _itemRepository.FindByIdAsync(item.ItemId),
                    Pack = pack
                };

                await _packItemRepository.AddAsync(packItem);
            }

            if (!string.IsNullOrEmpty(pack.DiscordChannelId))
            {
                pack.DiscordMessageId = await GenerateDiscordPackButton(pack);
                await _packRepository.CreateOrUpdateAsync(pack);
                await _packRepository.SaveAsync();
            }

            await _packItemRepository.SaveAsync();
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

            var packEntity = await _packRepository.FindByIdAsync(id);
            if (packEntity is null) throw new NotFoundException("Pack not found");

            if (packEntity.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            packEntity.StockPerPlayer = packDto.StockPerPlayer;
            packEntity.VipPrice = packDto.VipPrice ?? 0;
            packEntity.Price = packDto.Price ?? 0;
            packEntity.IsVipOnly = packDto.IsVipOnly ?? false;
            packEntity.DeliveryText = packDto.DeliveryText;
            packEntity.Description = packDto.Description;
            packEntity.IsWelcomePack = packDto.IsWelcomePack ?? false;
            packEntity.Name = packDto.Name;
            packEntity.Enabled = packDto.Enabled ?? false;
            packEntity.ImageUrl = packDto.ImageUrl;
            packEntity.IsBlockPurchaseRaidTime = packDto.IsBlockPurchaseRaidTime ?? false;

            await _packRepository.CreateOrUpdateAsync(packEntity);
            await _packRepository.SaveAsync();

            foreach (var packItem in packEntity.PackItems)
            {
                _packItemRepository.Delete(packItem);
            }
            await _packItemRepository.SaveAsync();

            foreach (var item in packDto.Items)
            {
                var packItem = new PackItem
                {
                    Amount = item.Amount,
                    Item = await _itemRepository.FindByIdAsync(item.ItemId),
                    Pack = packEntity
                };

                await _packItemRepository.CreateOrUpdateAsync(packItem);
                await _packItemRepository.SaveAsync();
            }

            if (!string.IsNullOrEmpty(packDto.DiscordChannelId))
            {
                if (packDto.DiscordChannelId != packEntity.DiscordChannelId && packEntity.DiscordMessageId != null)
                {
                    await _discordService.RemoveMessage(ulong.Parse(packEntity.DiscordChannelId!), packEntity.DiscordMessageId!.Value);
                }

                packEntity.DiscordChannelId = packDto.DiscordChannelId;
                packEntity.DiscordMessageId = await GenerateDiscordPackButton(packEntity);

                await _packRepository.CreateOrUpdateAsync(packEntity);
                await _packRepository.SaveAsync();
            }

            return await FetchPackById(id);
        }

        public async Task<Pack> DeletePackAsync(long id)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null) throw new NotFoundException("Package not found");

            if (pack.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            foreach (var packItem in pack.PackItems)
            {
                packItem.Deleted = DateTime.UtcNow;
                await _packItemRepository.CreateOrUpdateAsync(packItem);
            }
            await _packItemRepository.SaveAsync();

            if (pack.DiscordChannelId != null && pack.DiscordMessageId != null)
            {
                await _discordService.RemoveMessage(ulong.Parse(pack.DiscordChannelId!), pack.DiscordMessageId!.Value);
            }

            pack.Deleted = DateTime.UtcNow;
            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            return pack;
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
