using Microsoft.AspNetCore.Mvc;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/admin/shop")]
    public class ShopController : ControllerBase
    {
        private readonly ILogger<ShopController> _logger;

        public ShopController(ILogger<ShopController> logger)
        {
            _logger = logger;
        }


    }
}
