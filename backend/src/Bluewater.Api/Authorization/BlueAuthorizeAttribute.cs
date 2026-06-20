using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;

namespace Bluewater.Api.Authorization;

public class BlueAuthorizeAttribute : AuthorizeAttribute
{
    public BlueAuthorizeAttribute(BluePermission permission)
    {
        Policy = PermissionPolicyProvider.PolicyPrefix + permission;
    }
}
