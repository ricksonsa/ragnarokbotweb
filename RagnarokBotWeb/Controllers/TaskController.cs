using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("custom-tasks")]
        public async Task<IActionResult> CreateCustomTask(CustomTaskDto customTask)
        {
            return Ok(await _taskService.CreateTask(customTask));
        }

        [HttpPut("custom-tasks/{id}")]
        public async Task<IActionResult> UpdateCustomTask(long id, CustomTaskDto customTask)
        {
            return Ok(await _taskService.UpdateTask(id, customTask));
        }

        [HttpGet("custom-tasks")]
        public async Task<IActionResult> GetAllTasks([FromQuery] Paginator paginator, string? filter)
        {
            return Ok(await _taskService.GetTaskPageByFilterAsync(paginator, filter));
        }

        [HttpGet("custom-tasks/ids")]
        public async Task<IActionResult> GetAllTasksIds()
        {
            return Ok(await _taskService.GetAllTaskIds());
        }

        [HttpGet("custom-tasks/{id}")]
        public async Task<IActionResult> GetCustomTask(long id)
        {
            return Ok(await _taskService.FetchTaskById(id));
        }

        [HttpDelete("custom-tasks/{id}")]
        public async Task<IActionResult> DeleteCustomTask(long id)
        {
            return Ok(await _taskService.DeleteCustomTask(id));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackById(long id)
        {
            var task = await _taskService.FetchTaskById(id);
            if (task is null) return NotFound("Task not found");
            return Ok(task);
        }

        [ValidateAccessLevel(AccessLevel.Mod)]
        [HttpPatch("trigger")]
        public IActionResult TriggerJob(string jobId, string groupId)
        {
            _logger.Log(LogLevel.Debug, "PATCH Request to trigger job[{Job}] group[{Group}]", jobId, groupId);
            _taskService.TriggerJob(jobId, groupId);
            return Ok();
        }

        [HttpGet]
        public IActionResult Jobs()
        {
            _logger.Log(LogLevel.Debug, "GET Request to fetch scheduled jobs");
            return Ok(_taskService.ListJobs());
        }
    }
}
