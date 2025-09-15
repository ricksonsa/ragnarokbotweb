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
    [Route("api/packs")]
    public class PacksController : ControllerBase
    {
        private readonly ILogger<PacksController> _logger;
        private readonly IPackService _packService;

        public PacksController(ILogger<PacksController> logger, IPackService packService)
        {
            _logger = logger;
            _packService = packService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPacks([FromQuery] Paginator paginator, string? filter)
        {
            return Ok(await _packService.GetPacksPageByFilterAsync(paginator, filter));
        }


        [HttpGet("ids")]
        public async Task<IActionResult> GetAllPacksIds()
        {
            return Ok(await _packService.GetAllPacksIds());
        }

        [HttpGet("welcome-pack")]
        public async Task<IActionResult> GetWelcomePack()
        {
            var pack = await _packService.FetchWelcomePack();
            if (pack is null) return NotFound("Pack not found");
            return Ok(pack);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackById(long id)
        {
            var pack = await _packService.FetchPackById(id);
            if (pack is null) return NotFound("Pack not found");
            return Ok(pack);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePack(PackDto createPack)
        {
            var pack = await _packService.CreatePackAsync(createPack);
            return Ok(pack);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePack(long id, PackDto createPack)
        {
            var pack = await _packService.UpdatePackAsync(id, createPack);
            return Ok(pack);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePack(long id)
        {
            await _packService.DeletePackAsync(id);
            return Ok();
        }
    }
}
