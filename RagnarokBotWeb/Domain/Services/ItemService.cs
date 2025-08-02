using AutoMapper;
using RagnarokBotWeb.Application.Pagination;
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
        private readonly IMapper _mapper;

        public ItemService(IItemRepository itemRepository, ILogger<ItemService> logger, IMapper mapper)
        {
            _itemRepository = itemRepository;
            _logger = logger;
            _mapper = mapper;
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

        public async Task<Item?> FindItemByCodeAsync(string code)
        {
            return await _itemRepository.FindOneAsync(item => item.Code == code);
        }

        public async Task<Item?> FindItemByIdAsync(long id)
        {
            return await _itemRepository.FindByIdAsync(id);
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

        public async Task<Page<ItemDto>> GetItemsPageByFilterAsync(Paginator paginator, string? filter)
        {
            var page = await _itemRepository.GetPageByFilter(paginator, filter);
            return new Page<ItemDto>(page.Content.Select(_mapper.Map<ItemDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }
    }
}
