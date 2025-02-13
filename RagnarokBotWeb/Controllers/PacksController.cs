using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/admin/packs")]
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
        public async Task<IActionResult> GetAllPacks()
        {
            return Ok(await _packService.FetchAllPacksAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackById(long id)
        {
            var pack = await _packService.FetchPackById(id);
            if (pack is null) return NotFound("Pack not found");
            return Ok(pack);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePack(CreatePackDto createPack)
        {
            var pack = await _packService.CreatePackAsync(createPack);
            return Ok(pack);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePack(long id, CreatePackDto createPack)
        {
            var pack = await _packService.UpdatePackAsync(id, createPack);
            return Ok(pack);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePack(long id)
        {
            var pack = await _packService.DeletePackAsync(id);
            if (pack is null) return NotFound("Pack not found");
            return Ok(pack);
        }
    }
}
