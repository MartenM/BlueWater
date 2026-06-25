using Bluewater.Domain.Models.Groups;

namespace Bluewater.Core.Dto.Groups;

public record UserGroupInstanceDto(
    Guid Id,
    Guid UserGroupId,
    string UserGroupName,
    Guid UserGroupCategoryId,
    Guid SeasonId,
    string SeasonName,
    IReadOnlyList<UserGroupPermissionDto> Permissions,
    IReadOnlyList<InstanceMemberDto> Members);
