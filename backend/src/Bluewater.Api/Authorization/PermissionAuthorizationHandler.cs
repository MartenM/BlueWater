using Bluewater.Domain.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Bluewater.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (requirement.Permissions.Any(permission => context.User.HasClaim(BlueClaimTypes.Permission, permission.ToString())))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
