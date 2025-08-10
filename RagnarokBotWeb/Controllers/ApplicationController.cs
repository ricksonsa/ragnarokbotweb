using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/application")]
    public class ApplicationController : ControllerBase
    {
        private readonly ILogger<ApplicationController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ApplicationController(ILogger<ApplicationController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("version")]
        public async Task<IActionResult> GetItems([FromQuery] Paginator paginator, string? filter)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch all items by filter");
            var application = await _unitOfWork.AppDbContext.Config.FirstOrDefaultAsync();
            return Ok(new
            {
                Version = application?.Version ?? "1.0.0"
            });
        }
    }
}
