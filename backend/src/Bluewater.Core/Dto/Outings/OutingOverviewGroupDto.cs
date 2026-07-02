namespace Bluewater.Core.Dto.Outings;

public record OutingOverviewGroupDto(
    Guid UserGroupInstanceId,
    string UserGroupInstanceName,
    IReadOnlyList<OutingListItemDto> Outings);
