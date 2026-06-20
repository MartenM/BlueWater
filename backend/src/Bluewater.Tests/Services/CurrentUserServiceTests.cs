using System.Security.Claims;
using Bluewater.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bluewater.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void Constructor_ExtractsIdAndUsername_FromCurrentHttpContext()
    {
        var userId = Guid.NewGuid();
        var accessor = AccessorWithClaims(
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "user@example.com"));

        var sut = new CurrentUserService(accessor);

        sut.Id.ShouldBe(userId);
        sut.Username.ShouldBe("user@example.com");
    }

    [Fact]
    public void Constructor_DefaultsToEmpty_WhenHttpContextIsNull()
    {
        var accessor = new FakeHttpContextAccessor { HttpContext = null };

        var sut = new CurrentUserService(accessor);

        sut.Id.ShouldBe(Guid.Empty);
        sut.Username.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_DefaultsToEmpty_WhenUserHasNoClaims()
    {
        var accessor = AccessorWithClaims();

        var sut = new CurrentUserService(accessor);

        sut.Id.ShouldBe(Guid.Empty);
        sut.Username.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_DefaultsIdToEmpty_WhenSubClaimIsNotAGuid()
    {
        var accessor = AccessorWithClaims(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"));

        var sut = new CurrentUserService(accessor);

        sut.Id.ShouldBe(Guid.Empty);
    }

    private static FakeHttpContextAccessor AccessorWithClaims(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        return new FakeHttpContextAccessor { HttpContext = httpContext };
    }

    private class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
