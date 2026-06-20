using Bluewater.Domain.Auth;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using TokenOptions = Bluewater.Infra.Options.TokenOptions;

namespace Bluewater.Tests.Services;

public class TokenServiceTests
{
    private static readonly TokenOptions TokenOpts = new()
    {
        Secret = "test-signing-secret-at-least-32-bytes-long!!",
        Issuer = "bluewater-tests",
        Audience = "bluewater-tests",
        ExpireTime = TimeSpan.FromMinutes(15)
    };

    private readonly TokenService _sut = new(Options.Create(TokenOpts));

    [Fact]
    public void CreateAccessToken_IncludesUserIdentityClaims()
    {
        var user = new BlueUser { Id = Guid.NewGuid(), Email = "user@example.com" };

        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateAccessToken(user, []));

        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(user.Email);
    }

    [Fact]
    public void CreateAccessToken_IncludesPermissionClaims()
    {
        var user = new BlueUser { Id = Guid.NewGuid(), Email = "user@example.com" };
        var permissions = new List<BluePermission> { BluePermission.AdminViewGroups, BluePermission.AdminModifyGroups };

        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateAccessToken(user, permissions));

        token.GetPayloadValue<List<string>>(BlueClaimTypes.Permission)
            .ShouldBe(["ViewGroups", "ModifyGroups"]);
    }

    [Fact]
    public void CreateAccessToken_SetsIssuerAudienceAndExpiry()
    {
        var user = new BlueUser { Id = Guid.NewGuid(), Email = "user@example.com" };
        var before = DateTime.UtcNow;

        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateAccessToken(user, []));

        token.Issuer.ShouldBe(TokenOpts.Issuer);
        token.Audiences.ShouldContain(TokenOpts.Audience);
        token.ValidTo.ShouldBeInRange(before.Add(TokenOpts.ExpireTime).AddSeconds(-5), before.Add(TokenOpts.ExpireTime).AddSeconds(5));
    }

    [Fact]
    public void CreateAccessToken_GeneratesUniqueJtiPerCall()
    {
        var user = new BlueUser { Id = Guid.NewGuid(), Email = "user@example.com" };

        var first = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateAccessToken(user, []));
        var second = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateAccessToken(user, []));

        first.Id.ShouldNotBe(second.Id);
    }
}
