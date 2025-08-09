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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPackItemRepository _packItemRepository;
        private readonly IDiscordService _discordService;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public PackService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<PackService> logger,
            IPackRepository packRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IDiscordService discordService,
            IPackItemRepository packItemRepository,
            IUnitOfWork unitOfWork,
            IFileService fileService) : base(httpContextAccessor)
        {
            _logger = logger;
            _packRepository = packRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _discordService = discordService;
            _packItemRepository = packItemRepository;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<PackDto> CreatePackAsync(PackDto createPack)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            ValidateSubscription(server);

            var pack = _mapper.Map<Pack>(createPack);
            pack.ScumServer = server;
            if (!string.IsNullOrEmpty(pack.ImageUrl))
            {
                pack.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(pack.ImageUrl);
            }

            if (!string.IsNullOrEmpty(pack.DiscordChannelId))
            {
                pack.DiscordMessageId = await GenerateDiscordPackButton(pack);
            }

            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            return _mapper.Map<PackDto>(pack);
        }

        private static List<CreateEmbedField> GetFields(Pack pack)
        {
            List<CreateEmbedField> fields = [];
            if (pack.Price > 0) fields.Add(new CreateEmbedField("Price", pack.Price.ToString(), true));
            if (pack.VipPrice > 0) fields.Add(new CreateEmbedField("Vip Price", pack.VipPrice.ToString(), true));
            return fields;
        }

        private async Task<ulong> GenerateDiscordPackButton(Pack pack)
        {
            var action = $"buy_package:{pack.Id}";
            var embed = new CreateEmbed
            {
                Buttons = [new($"Buy {pack.Name}", action)],
                DiscordId = ulong.Parse(pack.DiscordChannelId!),
                GuildId = pack.ScumServer.Guild!.DiscordId,
                Text = pack.Description,
                Fields = GetFields(pack),
                Color = pack.IsVipOnly ? Color.Gold : Color.DarkPurple,
                ImageUrl = pack.ImageUrl,
                Title = pack.Name
            };

            IUserMessage message = await _discordService.SendEmbedToChannel(embed);
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
            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null || pack.Deleted != null) throw new NotFoundException("Package not found");

            return _mapper.Map<PackDto>(pack);
        }

        private void RemovePackItems(PackDto packDto, Pack existingPack)
        {
            var updatedItemIds = packDto.PackItems!.Select(wi => wi.ItemId).ToHashSet();

            foreach (var existingItem in existingPack.PackItems.ToList())
            {
                if (!updatedItemIds.Contains(existingItem.ItemId))
                {
                    _unitOfWork.AppDbContext.PackItems.Remove(existingItem);
                }
            }

            foreach (var dto in packDto.PackItems!)
            {
                if (!existingPack.PackItems.Any(wi => wi.ItemId == dto.ItemId))
                {
                    existingPack.PackItems.Add(new PackItem
                    {
                        ItemId = dto.ItemId,
                        Amount = dto.Amount,
                        AmmoCount = dto.AmmoCount,
                        PackId = existingPack.Id
                    });
                }
            }
        }

        public async Task<PackDto> UpdatePackAsync(long id, PackDto packDto)
        {
            var pack = await _packRepository.FindByIdAsync(id);
            if (pack == null)
                throw new NotFoundException("Pack not found");

            ValidateServerOwner(pack.ScumServer);
            ValidateSubscription(pack.ScumServer);

            var previousImage = pack.ImageUrl;
            var previousDiscordId = pack.DiscordChannelId;
            _mapper.Map(packDto, pack);

            if (!string.IsNullOrEmpty(pack.ImageUrl) && pack.ImageUrl != previousImage)
            {
                if (!string.IsNullOrEmpty(previousImage)) _fileService.DeleteFile(previousImage);
                pack.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(pack.ImageUrl);
            }

            RemovePackItems(packDto, pack);

            if (pack.Enabled)
            {
                try
                {
                    await _discordService.RemoveMessage(ulong.Parse(previousDiscordId!), pack.DiscordMessageId ?? 0);

                    pack.DiscordChannelId = packDto.DiscordChannelId;
                    pack.DiscordMessageId = await GenerateDiscordPackButton(pack);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error trying to create discord buttons for package {Pack} -> {Ex}", pack.Id, ex.Message);
                }

            }
            else
            {
                try
                {
                    await _discordService.RemoveMessage(ulong.Parse(previousDiscordId!), pack.DiscordMessageId ?? 0);
                }
                catch (Exception) { }
            }

            _unitOfWork.AppDbContext.Packs.Update(pack);
            await _unitOfWork.SaveAsync();

            return _mapper.Map<PackDto>(pack);
        }

        public async Task DeletePackAsync(long id)
        {
            var serverId = ServerId();

            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null) throw new NotFoundException("Package not found");

            ValidateServerOwner(pack.ScumServer);
            ValidateSubscription(pack.ScumServer);

            _packItemRepository.DeletePackItems(pack.PackItems);
            await _packItemRepository.SaveAsync();

            await DeleteDiscordMessage(pack);

            pack.Deleted = DateTime.UtcNow;
            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            try
            {
                if (!string.IsNullOrEmpty(pack.ImageUrl)) _fileService.DeleteFile(pack.ImageUrl);
            }
            catch (Exception)
            { }
            return;
        }

        public async Task<Page<PackDto>> GetPacksPageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _packRepository.GetPageByServerAndFilter(paginator, serverId!.Value, filter);
            return new Page<PackDto>(page.Content.Select(_mapper.Map<PackDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task<PackDto> FetchWelcomePack()
        {
            var serverId = ServerId();
            var pack = await _packRepository.FindOneAsync(package => package.IsWelcomePack && package.ScumServer.Id == serverId);
            if (pack == null) throw new NotFoundException("Package not found");

            return await FetchPackById(pack.Id);
        }
    }
}
