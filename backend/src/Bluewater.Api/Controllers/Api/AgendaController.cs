using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Agenda;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

/// <summary>
/// CRUD for agenda items. Listing/reading is open to anonymous callers, like NewsController -
/// agenda items have no privacy flag, so there's nothing to filter by caller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgendaController : ControllerBase
{
    private readonly IAgendaService _service;

    public AgendaController(IAgendaService service)
    {
        _service = service;
    }

    /// <summary>Lists agenda items, soonest first.</summary>
    [HttpGet]
    public Task<PagedResult<AgendaItemDto>> List(int page = 1, int pageSize = 20)
    {
        return _service.ListAsync(page, pageSize);
    }

    /// <summary>Lists agenda items whose span overlaps the given date range.</summary>
    [HttpGet("range")]
    public Task<IReadOnlyList<AgendaItemDto>> Range(DateOnly start, DateOnly end)
    {
        return _service.ListRangeAsync(start, end);
    }

    /// <summary>Lists the next upcoming agenda items.</summary>
    [HttpGet("upcoming")]
    public Task<IReadOnlyList<AgendaItemDto>> Upcoming(int count = 5)
    {
        return _service.ListUpcomingAsync(count);
    }

    /// <summary>Gets a single agenda item by id.</summary>
    [HttpGet("{id:guid}")]
    public Task<AgendaItemDto> Get(Guid id)
    {
        return _service.GetAsync(id);
    }

    /// <summary>Creates a new agenda item.</summary>
    [BlueAuthorize(BluePermission.AgendaModify)]
    [HttpPost]
    public Task<AgendaItemDto> Create(UpsertAgendaItemRequest request)
    {
        return _service.CreateAsync(request);
    }

    /// <summary>Updates an agenda item.</summary>
    [BlueAuthorize(BluePermission.AgendaModify)]
    [HttpPut("{id:guid}")]
    public Task<AgendaItemDto> Update(Guid id, UpsertAgendaItemRequest request)
    {
        return _service.UpdateAsync(id, request);
    }

    /// <summary>Deletes an agenda item.</summary>
    [BlueAuthorize(BluePermission.AgendaModify)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
