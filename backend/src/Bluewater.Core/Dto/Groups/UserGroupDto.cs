namespace Bluewater.Core.Dto.Groups;

public record UserGroupDto(
    Guid Id,
    string Name,
    string Description,
    Guid UserGroupCategoryId,
    string UserGroupCategoryName,
    IReadOnlyList<UserGroupPermissionDto> Permissions);
