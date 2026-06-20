using Bluewater.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bluewater.Core.Services;

public class CurrentUserService : ICurrentUserService
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var sub = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (sub != null && Guid.TryParse(sub, out var id))
        {
            Id = id;
        }

        Username = user?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
    }
}