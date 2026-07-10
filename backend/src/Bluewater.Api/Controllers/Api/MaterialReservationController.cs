using System.Globalization;
using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.MaterialPlanner;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class MaterialReservationController : ControllerBase
{
    private readonly IMaterialReservationService _materialReservationService;

    public MaterialReservationController(IMaterialReservationService materialReservationService)
    {
        _materialReservationService = materialReservationService;
    }

    /// <summary>Gets all bookable boats grouped by type, each with their reservations for the given day.</summary>
    // Route segment is "planner-day" rather than "day" to avoid colliding with AvailabilityController's
    // "day" operation in the generated NSwag client - both would otherwise generate a client method
    // named `day`, and NSwag silently keeps only the last one defined, breaking the other feature.
    [BlueAuthorize(BluePermission.MaterialPlannerUse)]
    [HttpGet("planner-day")]
    public Task<MaterialPlannerDayDto> GetDay([FromQuery] string date)
    {
        return _materialReservationService.GetDayAsync(ParseDateOnly(date));
    }

    /// <summary>Checks whether a boat is already reserved for a given date/time range.</summary>
    [BlueAuthorize(BluePermission.MaterialPlannerUse)]
    [HttpGet("conflict")]
    public Task<MaterialReservationConflictDto> GetConflict(
        [FromQuery] Guid equipmentId,
        [FromQuery] string date,
        [FromQuery] string startTime,
        [FromQuery] string endTime)
    {
        return _materialReservationService.GetConflictAsync(equipmentId, ParseDateOnly(date), ParseTimeOnly(startTime), ParseTimeOnly(endTime));
    }

    /// <summary>Creates a new reservation for the current user.</summary>
    [BlueAuthorize(BluePermission.MaterialPlannerUse)]
    [HttpPost]
    public Task<MaterialReservationDto> Create([FromBody] CreateMaterialReservationRequest request)
    {
        return _materialReservationService.CreateAsync(request);
    }

    /// <summary>Moves or resizes an existing reservation (owner or MaterialPlannerOverride only).</summary>
    [BlueAuthorize(BluePermission.MaterialPlannerUse)]
    [HttpPatch("{id:guid}")]
    public Task<MaterialReservationDto> Update(Guid id, [FromBody] UpdateMaterialReservationRequest request)
    {
        return _materialReservationService.UpdateAsync(id, request);
    }

    /// <summary>Deletes a reservation (owner or MaterialPlannerOverride only).</summary>
    [BlueAuthorize(BluePermission.MaterialPlannerUse)]
    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id)
    {
        return _materialReservationService.DeleteAsync(id);
    }

    /// <summary>Sets or clears a reservation's custom display label (owner or MaterialPlannerOverride only).</summary>
    [BlueAuthorize(BluePermission.MaterialPlannerUse)]
    [HttpPut("{id:guid}/label")]
    public Task<MaterialReservationDto> SetLabel(Guid id, [FromBody] SetMaterialReservationLabelRequest request)
    {
        return _materialReservationService.SetLabelAsync(id, request);
    }

    // Query-string dates are passed as plain "yyyy-MM-dd" strings (not a JS Date/ISO datetime)
    // specifically to avoid UTC-conversion day-shift bugs across timezones.
    private static DateOnly ParseDateOnly(string value)
    {
        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static TimeOnly ParseTimeOnly(string value)
    {
        return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}
