namespace Bluewater.Core.Dto.Groups;

public record UserGroupMembershipDto(
    Guid GroupId,
    string SeasonDisplayName,
    string GroupCategoryName,
    string GroupName,
    string? RoleName);
