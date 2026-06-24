using Bluewater.Core.Dto.Seasons;
using Bluewater.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// Read-only access to the available seasons. Seasons are created out-of-band
/// (not via this API), so there's no create/update/delete here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonService _service;

    public SeasonsController(ISeasonService service)
    {
        _service = service;
    }

    /// <summary>Lists all seasons, most recent first, flagging the current one.</summary>
    [Authorize]
    [HttpGet]
    public Task<List<SeasonDto>> List()
    {
        return _service.ListAsync();
    }

    /// <summary>Gets a single season by id.</summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    public Task<SeasonDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }
}
