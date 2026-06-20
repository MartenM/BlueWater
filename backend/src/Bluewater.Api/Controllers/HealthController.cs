using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public Task<string> Get()
    {
        return Task.FromResult("OK");
    }
}