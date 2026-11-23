using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace MediBook.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "Healthy",
                message = "MediBook API is running"
            });
        }
    }
}

