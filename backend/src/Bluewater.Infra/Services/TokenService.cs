using System.Security.Claims;
using System.Text;
using Bluewater.Domain.Auth;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bluewater.Infra.Services;

public class TokenService
{
    private IOptions<TokenOptions> _options;

    public TokenService(IOptions<TokenOptions> options)
    {
        _options = options;
    }

    public string CreateAccessToken(BlueUser user, IReadOnlyCollection<BluePermission> permissions)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Value.Secret)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email ?? string.Empty,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [BlueClaimTypes.Permission] = permissions.Select(p => p.ToString()).ToList()
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Value.Issuer,
            Audience = _options.Value.Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.Add(_options.Value.ExpireTime),
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();

        var token = handler.CreateToken(descriptor);

        return token;
    }
}