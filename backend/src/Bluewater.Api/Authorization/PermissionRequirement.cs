using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;

namespace Bluewater.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public BluePermission Permission { get; }

    public PermissionRequirement(BluePermission permission)
    {
        Permission = permission;
    }
}
