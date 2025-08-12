using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> GetItems()
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch all items by filter");
            var application = await _unitOfWork.AppDbContext.Config.FirstOrDefaultAsync();
            return Ok(new
            {
                Version = application?.Version ?? "1.0.0"
            });
        }

        [HttpGet("timezones")]
        public IActionResult GetTimezones()
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch all timezones");
            var timeZoneIds = TimeZoneInfo
              .GetSystemTimeZones()
              .Select(tz => new
              {
                  Id = tz.Id,
                  DisplayName = tz.DisplayName
              })
              .ToList();
            return Ok(timeZoneIds);
        }
    }
}
