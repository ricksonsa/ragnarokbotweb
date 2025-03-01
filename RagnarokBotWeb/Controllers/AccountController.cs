using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;

        public AccountController(IUserService userService, ILogger<AccountController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(AuthenticateDto auth)
        {
            _logger.LogInformation("Post request for pre-authenticating user: " + auth.Email);
            var token = await _userService.PreAuthenticate(auth);
            return Ok(token);
        }

        [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.IdTokenPolicy)]
        [HttpGet("login")]
        public async Task<IActionResult> Authenticate(long serverId)
        {
            _logger.LogInformation("Post request for authenticating user for serverId: " + serverId);
            var token = await _userService.Authenticate(serverId);
            return Ok(token);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto register)
        {
            _logger.LogInformation("Post request for registering user: " + register.Email);
            UserDto user = await _userService.Register(register);
            return Ok(user);
        }
    }
}
