using System.ComponentModel.DataAnnotations;

namespace Bluewater.Core.Dto.Groups;

public record UpsertUserGroupCategoryRequest(
    [Required] string Name,
    string Description);
