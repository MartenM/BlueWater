namespace Bluewater.Core.Dto.Groups;

public record ReorderRolesRequest(IReadOnlyList<Guid> RoleIds);
