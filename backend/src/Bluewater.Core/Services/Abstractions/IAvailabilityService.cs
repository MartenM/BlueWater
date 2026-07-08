using Bluewater.Core.Dto.Availability;

namespace Bluewater.Core.Services.Abstractions;

public interface IAvailabilityService
{
    Task<MyWeekAvailabilityDto> GetMyWeekAsync(DateOnly weekStart);
    Task<List<AvailabilityBlockDto>> SetMyDayAsync(SetDayAvailabilityRequest request);
    Task<InstanceWeekAvailabilityDto> GetInstanceWeekAsync(Guid instanceId, DateOnly weekStart);
}
