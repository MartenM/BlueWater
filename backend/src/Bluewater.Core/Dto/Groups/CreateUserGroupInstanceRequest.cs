using System.ComponentModel.DataAnnotations;

namespace Bluewater.Core.Dto.Groups;

public record CreateUserGroupInstanceRequest(
    [Required] Guid UserGroupId,
    [Required] Guid SeasonId);
