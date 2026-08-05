using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase

    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is working 🚀");
        }
    }
}
