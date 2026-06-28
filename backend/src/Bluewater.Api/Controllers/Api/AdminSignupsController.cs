using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/admin/signups")]
public class AdminSignupsController : ControllerBase
{
    private readonly ISignupService _service;

    public AdminSignupsController(ISignupService service)
    {
        _service = service;
    }

    [BlueAuthorize(BluePermission.AdminSignupView, BluePermission.AdminSignupModify)]
    [HttpGet]
    public Task<List<SignupListItemDto>> List() => _service.AdminListAsync();

    [BlueAuthorize(BluePermission.AdminSignupView, BluePermission.AdminSignupModify)]
    [HttpGet("{id:guid}")]
    public Task<SignupAdminDetailDto> Get(Guid id) => _service.AdminGetAsync(id);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPost]
    public Task<SignupAdminDetailDto> Create(UpsertSignupRequest request) => _service.AdminCreateAsync(request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPut("{id:guid}")]
    public Task<SignupAdminDetailDto> Update(Guid id, UpsertSignupRequest request) => _service.AdminUpdateAsync(id, request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id) => _service.AdminDeleteAsync(id);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPost("{id:guid}/fields")]
    public Task<SignupInputFieldDto> AddField(Guid id, UpsertSignupInputFieldRequest request)
        => _service.AdminAddFieldAsync(id, request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPut("{id:guid}/fields/{fieldId:guid}")]
    public Task<SignupInputFieldDto> UpdateField(Guid id, Guid fieldId, UpsertSignupInputFieldRequest request)
        => _service.AdminUpdateFieldAsync(id, fieldId, request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpDelete("{id:guid}/fields/{fieldId:guid}")]
    public Task DeleteField(Guid id, Guid fieldId) => _service.AdminDeleteFieldAsync(id, fieldId);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPut("{id:guid}/fields/reorder")]
    public Task ReorderFields(Guid id, ReorderFieldsRequest request) => _service.AdminReorderFieldsAsync(id, request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpPatch("{id:guid}/responses/{responseId:guid}/reservation")]
    public Task<SignupResponseDto> SetReservation(Guid id, Guid responseId, SetReservationRequest request)
        => _service.AdminSetReservationAsync(id, responseId, request);

    [BlueAuthorize(BluePermission.AdminSignupModify)]
    [HttpDelete("{id:guid}/responses/{responseId:guid}")]
    public Task DeleteResponse(Guid id, Guid responseId) => _service.AdminDeleteResponseAsync(id, responseId);

    [BlueAuthorize(BluePermission.AdminSignupView, BluePermission.AdminSignupModify)]
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id)
    {
        var bytes = await _service.AdminExportCsvAsync(id);
        return File(bytes, "text/csv", $"signup-{id}.csv");
    }
}
