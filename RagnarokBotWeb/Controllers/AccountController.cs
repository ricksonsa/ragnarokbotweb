using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(AuthenticateDto auth)
        {
            _logger.LogInformation("Post request for authenticating user: " + auth.Email);
            var token = await _userService.Authenticate(auth);
            if (token is null) return Unauthorized();
            return Ok(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto register)
        {
            _logger.LogInformation("Post request for registering user: " + register.Email);
            UserDto user = await _userService.Register(register);
            return Ok(user);
        }
    }
}
