using System.Globalization;
using Bluewater.Api.Authorization;
using Bluewater.Core.Dto.Availability;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>Gets the current user's own availability for the week containing the given date.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("my-week")]
    public Task<MyWeekAvailabilityDto> GetMyWeek([FromQuery] string weekStart)
    {
        return _availabilityService.GetMyWeekAsync(ParseDateOnly(weekStart));
    }

    /// <summary>Replaces the current user's availability blocks for a single day.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpPut("my-week/day")]
    public Task<List<AvailabilityBlockDto>> SetMyDay([FromBody] SetDayAvailabilityRequest request)
    {
        return _availabilityService.SetMyDayAsync(request);
    }

    /// <summary>Gets a team's members grouped by role, each with their availability for the week containing the given date.</summary>
    [BlueAuthorize(BluePermission.OutingPlannerUse)]
    [HttpGet("instances/{instanceId:guid}/availability")]
    public Task<InstanceWeekAvailabilityDto> GetInstanceWeek(Guid instanceId, [FromQuery] string weekStart)
    {
        return _availabilityService.GetInstanceWeekAsync(instanceId, ParseDateOnly(weekStart));
    }

    // Query-string dates are passed as plain "yyyy-MM-dd" strings (not a JS Date/ISO datetime)
    // specifically to avoid UTC-conversion day-shift bugs across timezones.
    private static DateOnly ParseDateOnly(string value)
    {
        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
