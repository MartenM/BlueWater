using Bluewater.Domain.Models.Groups;

namespace Bluewater.Core.Dto.Groups;

public record AssignGroupPermissionRequest(BluePermission Permission, Guid? UserGroupCategoryRoleId);
