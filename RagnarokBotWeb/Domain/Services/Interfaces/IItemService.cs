using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IItemService
    {
        Task<Item> CreateItemAsync(ItemDto createItem);
        Task<Item?> UpdateItemAsync(long id, ItemDto itemDto);
        Task<Item?> ActivateItemAsync(long id);
        Task<Item?> DeactivateItemAsync(long id);
        Task<IEnumerable<Item>> FetchAllItemsAsync();
        Task<Page<ItemDto>> GetItemsPageByFilterAsync(Paginator paginator, string? filter);
        Task<Item?> FindItemByNameAsync(string name);
        Task<Item?> FindItemByIdAsync(long id);
        Task<Item?> DeleteItemAsync(long id);
    }
}
