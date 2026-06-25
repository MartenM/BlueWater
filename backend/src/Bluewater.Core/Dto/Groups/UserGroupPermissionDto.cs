using Bluewater.Domain.Models.Groups;

namespace Bluewater.Core.Dto.Groups;

public record UserGroupPermissionDto(BluePermission Permission, Guid? UserGroupCategoryRoleId);
