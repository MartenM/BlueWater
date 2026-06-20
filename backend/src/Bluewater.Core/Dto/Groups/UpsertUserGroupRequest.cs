using System.ComponentModel.DataAnnotations;

namespace Bluewater.Core.Dto.Groups;

public record UpsertUserGroupRequest(
    [Required] string Name,
    string Description,
    [Required] Guid UserGroupCategoryId);
