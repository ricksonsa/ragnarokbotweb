using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class ItemService : IItemService
    {
        private readonly ILogger<ItemService> _logger;
        private readonly IItemRepository _itemRepository;

        public ItemService(IItemRepository itemRepository, ILogger<ItemService> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<Item> CreateItemAsync(ItemDto createItem)
        {
            var item = new Item
            {
                Id = 0,
                Active = true,
                Code = createItem.Code,
                Name = createItem.Name
            };

            await _itemRepository.AddAsync(item);
            await _itemRepository.SaveAsync();
            return item;
        }

        public async Task<Item?> UpdateItemAsync(long id, ItemDto itemDto)
        {
            var item = await FindItemByIdAsync(id);
            if (item is null) return null;
            item.Code = itemDto.Code;
            item.Name = itemDto.Name;
            _itemRepository.Update(item);
            await _itemRepository.SaveAsync();
            return item;
        }

        public async Task<Item?> ActivateItemAsync(long id)
        {
            var item = await FindItemByIdAsync(id);
            if (item is null) return null;
            item.Active = true;
            _itemRepository.Update(item);
            await _itemRepository.SaveAsync();
            return item;
        }

        public async Task<Item?> DeactivateItemAsync(long id)
        {
            var item = await FindItemByIdAsync(id);
            if (item is null) return null;
            item.Active = false;
            _itemRepository.Update(item);
            await _itemRepository.SaveAsync();
            return item;
        }

        public async Task<Item?> FindItemByNameAsync(string name)
        {
            return await _itemRepository.FindOneAsync(item => item.Name == name);
        }

        public async Task<Item?> FindItemByIdAsync(long id)
        {
            return await _itemRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Item>> FetchAllItemsAsync()
        {
            return await _itemRepository.GetAllAsync();
        }

        public async Task<Item?> DeleteItemAsync(long id)
        {
            var item = await FindItemByIdAsync(id);
            if (item is null) return null;
            _itemRepository.Delete(item);
            await _itemRepository.SaveAsync();
            return item;
        }
    }
}
