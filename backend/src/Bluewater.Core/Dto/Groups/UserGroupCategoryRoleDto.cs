namespace Bluewater.Core.Dto.Groups;

public record UserGroupCategoryRoleDto(
    Guid Id,
    Guid UserGroupCategoryId,
    string NamePlural,
    string? NameMasculine,
    string? NameFeminine);
