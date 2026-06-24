namespace Bluewater.Core.Dto.Groups;

public record UserGroupOverviewDto(Guid Id, string Name, Guid? InstanceId, int? MemberCount, int? PermissionCount);

public record UserGroupCategoryOverviewDto(
    Guid Id,
    string Name,
    string Description,
    int GroupCount,
    IReadOnlyList<UserGroupOverviewDto> Groups);
