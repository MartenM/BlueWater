using Bluewater.Domain.Auth;
using Bluewater.Domain.Models.Groups;
using Hangfire.Dashboard;

namespace Bluewater.Api.Authorization;

public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.HasClaim(BlueClaimTypes.Permission, BluePermission.AdminModifySettings.ToString());
    }
}
