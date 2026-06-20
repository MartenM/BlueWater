using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;

namespace Bluewater.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<BluePermission> Permissions { get; }

    public PermissionRequirement(IReadOnlyCollection<BluePermission> permissions)
    {
        Permissions = permissions;
    }
}
