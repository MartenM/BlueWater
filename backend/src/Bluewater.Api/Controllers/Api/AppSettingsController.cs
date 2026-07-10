using Bluewater.Core.Dto.AppSettings;
using Bluewater.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AppSettingsController : ControllerBase
{
    private readonly IAppSettingsService _appSettingsService;

    public AppSettingsController(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
    }

    /// <summary>Gets the global material planner timeline hour range.</summary>
    [Authorize]
    [HttpGet("material-planner")]
    public Task<MaterialPlannerSettingsDto> GetMaterialPlanner()
    {
        return _appSettingsService.GetMaterialPlannerSettingsAsync();
    }
}
