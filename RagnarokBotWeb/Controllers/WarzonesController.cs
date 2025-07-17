using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/warzones")]
    public class WarzonesController : ControllerBase
    {
        private readonly ILogger<WarzonesController> _logger;
        private readonly IWarzoneService _warzoneService;

        public WarzonesController(ILogger<WarzonesController> logger, IWarzoneService warzoneService)
        {
            _logger = logger;
            _warzoneService = warzoneService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllWarzones([FromQuery] Paginator paginator, string? filter)
        {
            return Ok(await _warzoneService.GetWarzonePageByFilterAsync(paginator, filter));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWarzoneById(long id)
        {
            var warzone = await _warzoneService.FetchWarzoneById(id);
            return Ok(warzone);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWarzone(WarzoneDto createWarzone)
        {
            var warzone = await _warzoneService.CreateWarzoneAsync(createWarzone);
            return Ok(warzone);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWarzone(long id, WarzoneDto createWarzone)
        {
            var warzone = await _warzoneService.UpdateWarzoneAsync(id, createWarzone);
            return Ok(warzone);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarzone(long id)
        {
            await _warzoneService.DeleteWarzoneAsync(id);
            return Ok();
        }
    }
}
