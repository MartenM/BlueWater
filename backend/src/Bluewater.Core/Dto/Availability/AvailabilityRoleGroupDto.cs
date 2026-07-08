namespace Bluewater.Core.Dto.Availability;

public record AvailabilityRoleGroupDto(
    Guid? UserGroupCategoryRoleId,
    string RoleLabel,
    List<AvailabilityMemberWeekDto> Members);
