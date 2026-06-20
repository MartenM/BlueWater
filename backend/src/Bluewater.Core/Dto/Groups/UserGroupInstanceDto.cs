using Bluewater.Domain.Models.Groups;

namespace Bluewater.Core.Dto.Groups;

public record UserGroupInstanceDto(
    Guid Id,
    Guid UserGroupId,
    string UserGroupName,
    Guid SeasonId,
    string SeasonName,
    IReadOnlyList<BluePermission> Permissions,
    IReadOnlyList<Guid> MemberUserIds);
