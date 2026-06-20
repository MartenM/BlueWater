using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;

namespace Bluewater.Api.Authorization;

public class BlueAuthorizeAttribute : AuthorizeAttribute
{
    public BlueAuthorizeAttribute(params BluePermission[] permissions)
    {
        Policy = PermissionPolicyProvider.PolicyPrefix + string.Join(',', permissions);
    }
}
