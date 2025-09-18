using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Filters;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/items")]
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
        [ValidateAccessLevel(AccessLevel.Mod)]
        public async Task<IActionResult> CreateItem([FromBody] ItemDto item)
        {
            _logger.Log(LogLevel.Debug, "REST Request for creating a new Item with Data: " + JsonConvert.SerializeObject(item));
            return Ok(await _itemService.CreateItemAsync(item));
        }

        [HttpPut("{id}")]
        [ValidateAccessLevel(AccessLevel.Mod)]
        public async Task<IActionResult> UpdateItem(long id, ItemDto createItem)
        {
            _logger.Log(LogLevel.Information, "REST Request for updating an Item with Data: " + JsonConvert.SerializeObject(createItem));
            var item = await _itemService.UpdateItemAsync(id, createItem);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpPatch("{id}/activate")]
        [ValidateAccessLevel(AccessLevel.Mod)]
        public async Task<IActionResult> ActivateItem(long id)
        {
            _logger.Log(LogLevel.Debug, "REST Request for activating an Item with Id: " + id);
            var item = await _itemService.ActivateItemAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpPatch("{id}/deactivate")]
        [ValidateAccessLevel(AccessLevel.Mod)]
        public async Task<IActionResult> DeactivateItem(long id)
        {
            _logger.Log(LogLevel.Debug, "REST Request for deactivating an Item with Id: " + id);
            var item = await _itemService.DeactivateItemAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetItems([FromQuery] Paginator paginator, string? filter)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch all items by filter");
            var items = await _itemService.GetItemsPageByFilterAsync(paginator, filter);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemById(long id)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch with id: " + id);
            var item = await _itemService.FindItemByIdAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }


        [HttpDelete("{id}")]
        [ValidateAccessLevel(AccessLevel.Mod)]
        public async Task<IActionResult> DeleteItem(long id)
        {
            _logger.Log(LogLevel.Debug, "REST Request to delete with id: " + id);
            var item = await _itemService.DeleteItemAsync(id);
            if (item is null) return NotFound("Item not found");
            return Ok(item);
        }
    }
}
