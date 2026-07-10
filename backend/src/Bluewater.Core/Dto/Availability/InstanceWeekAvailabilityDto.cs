namespace Bluewater.Core.Dto.Availability;

public record InstanceWeekAvailabilityDto(
    Guid UserGroupInstanceId,
    DateOnly WeekStart,
    List<AvailabilityRoleGroupDto> RoleGroups,
    List<OutingTimelineEntryDto> Outings);
