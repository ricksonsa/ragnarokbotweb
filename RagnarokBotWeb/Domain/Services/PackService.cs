using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class PackService : IPackService
    {

        private readonly ILogger<PackService> _logger;

        private readonly IPackRepository _packRepository;
        private readonly IPackItemRepository _packItemRepository;
        private readonly IItemRepository _itemRepository;

        public PackService(ILogger<PackService> logger, IPackRepository packRepository, IPackItemRepository packItemRepository, IItemRepository itemRepository)
        {
            _logger = logger;
            _packRepository = packRepository;
            _packItemRepository = packItemRepository;
            _itemRepository = itemRepository;
        }

        public async Task<PackDto> CreatePackAsync(CreatePackDto createPack)
        {
            var pack = new Pack
            {
                Description = createPack.Description,
                Name = createPack.Name,
                Price = createPack.Price,
                VipPrice = createPack.VipPrice
            };

            await _packRepository.AddAsync(pack);
            await _packRepository.SaveAsync();

            var packDto = new PackDto
            {
                Id = pack.Id,
                Description = createPack.Description,
                Name = createPack.Name,
                Price = createPack.Price,
                VipPrice = createPack.VipPrice,
                Items = []
            };

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

                packDto.Items.Add(item);
            });

            return packDto;
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

        public async Task<PackDto> UpdatePackAsync(long id, CreatePackDto createPack)
        {
            var pack = await _packRepository.FindByIdAsync(id);
            if (pack is null) return null;

            pack.Description = createPack.Description;
            pack.Name = createPack.Name;
            pack.Price = createPack.Price;
            pack.VipPrice = createPack.VipPrice;

            _packRepository.Update(pack);
            await _packRepository.SaveAsync();

            foreach (var packItem in pack.PackItems)
            {
                _packItemRepository.Delete(packItem);
            }
            await _packItemRepository.SaveAsync();

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
