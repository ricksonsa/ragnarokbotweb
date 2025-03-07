using AutoMapper;
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
        private readonly IMapper _mapper;

        public PackService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<PackService> logger,
            IPackRepository packRepository,
            IPackItemRepository packItemRepository,
            IItemRepository itemRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository) : base(httpContextAccessor)
        {
            _logger = logger;
            _packRepository = packRepository;
            _packItemRepository = packItemRepository;
            _itemRepository = itemRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
        }

        public async Task<PackDto> CreatePackAsync(PackDto createPack)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            var pack = _mapper.Map<Pack>(createPack);
            pack.ScumServer = server;
            await _packRepository.AddAsync(pack);
            await _packRepository.SaveAsync();

            createPack.Items.ForEach(async item =>
            {
                var packItem = new PackItem
                {
                    Amount = item.Amount,
                    Item = await _itemRepository.FindByIdAsync(item.ItemId),
                    Pack = pack
                };

                await _packItemRepository.AddAsync(packItem);
                await _packItemRepository.SaveAsync();
            });

            return _mapper.Map<PackDto>(pack);
        }

        public async Task<PackDto> FetchPackById(long id)
        {
            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null) return null;
            var packDto = new PackDto
            {
                Id = id,
                Description = pack.Description,
                Name = pack.Name,
                Price = pack.Price,
                VipPrice = pack.VipPrice,
                Items = pack.PackItems.Select(packItem => new ItemToPackDto
                {
                    Amount = packItem.Amount,
                    ItemCode = packItem.Item.Code,
                    ItemId = packItem.Item.Id,
                    ItemName = packItem.Item.Name
                }).ToList()
            };

            return packDto;
        }

        public async Task<PackDto> UpdatePackAsync(long id, PackDto packDto)
        {
            var packEntity = await _packRepository.FindByIdAsync(id);
            if (packEntity is null) throw new NotFoundException("Pack not found");

            var pack = _mapper.Map<Pack>(packEntity);
            pack.ScumServer = packEntity.ScumServer;

            await _packRepository.CreateOrUpdateAsync(pack);
            await _packRepository.SaveAsync();

            foreach (var packItem in pack.PackItems)
            {
                _packItemRepository.Delete(packItem);
            }
            await _packItemRepository.SaveAsync();

            packDto.Items.ForEach(async item =>
            {
                var packItem = new PackItem
                {
                    Amount = item.Amount,
                    Item = await _itemRepository.FindByIdAsync(item.ItemId),
                    Pack = pack
                };

                await _packItemRepository.CreateOrUpdateAsync(packItem);
                await _packItemRepository.SaveAsync();
            });

            return await FetchPackById(id);
        }

        public async Task<Pack> DeletePackAsync(long id)
        {
            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null) return null;

            foreach (var packItem in pack.PackItems)
            {
                _packItemRepository.Delete(packItem);
            }
            await _packItemRepository.SaveAsync();

            _packRepository.Delete(pack);
            await _packRepository.SaveAsync();

            return pack;
        }

        public async Task<IEnumerable<PackDto>> FetchAllPacksAsync()
        {
            return (await _packRepository.GetAllAsync()).Select(pack =>
            {
                return new PackDto
                {
                    Id = pack.Id,
                    Description = pack.Description,
                    Name = pack.Name,
                    Price = pack.Price,
                    VipPrice = pack.VipPrice,
                    Items = pack.PackItems.Select(packItem => new ItemToPackDto
                    {
                        Amount = packItem.Amount,
                        ItemCode = packItem.Item.Code,
                        ItemId = packItem.Item.Id,
                        ItemName = packItem.Item.Name
                    }).ToList()
                };
            });
        }
    }
}
