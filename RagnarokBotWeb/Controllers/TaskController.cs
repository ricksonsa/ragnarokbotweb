using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/tasks")]
    public class TaskController : ControllerBase
    {
        private readonly ILogger<TaskController> _logger;
        private readonly ITaskService _taskService;

        public TaskController(ILogger<TaskController> logger, ITaskService taskService)
        {
            _logger = logger;
            _taskService = taskService;
        }

        [HttpPatch("trigger")]
        public async Task<IActionResult> TriggerJob(string jobId, string groupId)
        {
            _logger.Log(LogLevel.Debug, "PATCH Request to trigger job[{Job}] group[{Group}]", jobId, groupId);
            await _taskService.TriggerJob(jobId, groupId);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Jobs()
        {
            _logger.Log(LogLevel.Debug, "GET Request to fetch scheduled jobs");
            return Ok(await _taskService.ListJobs());
        }
    }
}
