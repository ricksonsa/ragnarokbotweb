using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/admin/items")]
    public class ItemsController : ControllerBase
    {
        private readonly ILogger<ItemsController> _logger;
        private readonly IItemService _itemService;

        public ItemsController(ILogger<ItemsController> logger, IItemService itemService)
        {
            _logger = logger;
            _itemService = itemService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem(ItemDto createItem)
        {
            _logger.Log(LogLevel.Information, "REST Request for creating a new Item with Data: " + JsonConvert.SerializeObject(createItem));
            return Ok(await _itemService.CreateItemAsync(createItem));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(long id, ItemDto createItem)
        {
            _logger.Log(LogLevel.Information, "REST Request for updating an Item with Data: " + JsonConvert.SerializeObject(createItem));
            var item = await _itemService.UpdateItemAsync(id, createItem);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> ActivateItem(long id)
        {
            _logger.Log(LogLevel.Information, "REST Request for activating an Item with Id: " + id);
            var item = await _itemService.ActivateItemAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateItem(long id)
        {
            _logger.Log(LogLevel.Information, "REST Request for deactivating an Item with Id: " + id);
            var item = await _itemService.DeactivateItemAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            _logger.Log(LogLevel.Information, "REST Request to fetch all items");
            var items = await _itemService.FetchAllItemsAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemById(long id)
        {
            _logger.Log(LogLevel.Information, "REST Request to fetch with id: " + id);
            var item = await _itemService.FindItemByIdAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetItemById(string name)
        {
            _logger.Log(LogLevel.Information, "REST Request to fetch with name: " + name);
            var item = await _itemService.FindItemByNameAsync(name);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(long id)
        {
            _logger.Log(LogLevel.Information, "REST Request to delete with id: " + id);
            var item = await _itemService.DeleteItemAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }
    }
}
