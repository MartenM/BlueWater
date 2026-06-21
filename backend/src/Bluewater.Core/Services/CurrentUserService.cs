using System.Security.Claims;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Auth;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bluewater.Core.Services;

public class CurrentUserService : ICurrentUserService, ICurrentUserAccessor
{
    private readonly ClaimsPrincipal? _user;

    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;

    Guid? ICurrentUserAccessor.UserId => Id == Guid.Empty ? null : Id;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;

        var sub = _user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (sub != null && Guid.TryParse(sub, out var id))
        {
            Id = id;
        }

        Username = _user?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
    }

    public bool HasPermission(BluePermission permission) =>
        _user?.HasClaim(BlueClaimTypes.Permission, permission.ToString()) ?? false;
}