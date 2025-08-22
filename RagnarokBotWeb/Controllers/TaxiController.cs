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
    [Route("api/taxis")]
    public class TaxiController : ControllerBase
    {
        private readonly ILogger<TaxiController> _logger;
        private readonly ITaxiService _taxiService;

        public TaxiController(ILogger<TaxiController> logger, ITaxiService taxiService)
        {
            _logger = logger;
            _taxiService = taxiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTaxis([FromQuery] Paginator paginator, string? filter)
        {
            return Ok(await _taxiService.GetTaxisPageByFilterAsync(paginator, filter));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var pack = await _taxiService.FetchTaxiById(id);
            if (pack is null) return NotFound("Taxi not found");
            return Ok(pack);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTaxi(TaxiDto dto)
        {
            var pack = await _taxiService.CreateTaxiAsync(dto);
            return Ok(pack);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTaxi(long id, TaxiDto dto)
        {
            var pack = await _taxiService.UpdateTaxiAsync(id, dto);
            return Ok(pack);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaxi(long id)
        {
            await _taxiService.DeleteTaxiAsync(id);
            return Ok();
        }
    }
}
