using Bluewater.Core.Dto;
using Bluewater.Core.Services;
using Bluewater.Infra.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Bluewater.Tests.Services;

public class CookieAuthServiceTests
{
    private static readonly CookieAuthOptions Options = new()
    {
        AccessTokenCookieName = "bw_access_token",
        RefreshTokenCookieName = "bw_refresh_token",
        Secure = false,
        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
        Domain = null,
        Path = "/"
    };

    [Fact]
    public void SetAuthCookies_AppendsAccessAndRefreshTokenCookies()
    {
        var context = new DefaultHttpContext();
        var sut = new CookieAuthService(new FakeHttpContextAccessor { HttpContext = context }, Microsoft.Extensions.Options.Options.Create(Options));

        sut.SetAuthCookies(new AuthResponse("access-value", "refresh-value"));

        var setCookieHeaders = context.Response.Headers[HeaderNames.SetCookie];
        setCookieHeaders.Count.ShouldBe(2);
        setCookieHeaders.ShouldContain(h => h!.StartsWith("bw_access_token=access-value"));
        setCookieHeaders.ShouldContain(h => h!.StartsWith("bw_refresh_token=refresh-value"));
    }

    [Fact]
    public void GetRefreshTokenFromCookie_ReturnsValue_WhenCookiePresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderNames.Cookie] = "bw_refresh_token=abc123";
        var sut = new CookieAuthService(new FakeHttpContextAccessor { HttpContext = context }, Microsoft.Extensions.Options.Options.Create(Options));

        sut.GetRefreshTokenFromCookie().ShouldBe("abc123");
    }

    [Fact]
    public void GetRefreshTokenFromCookie_ReturnsNull_WhenCookieAbsent()
    {
        var context = new DefaultHttpContext();
        var sut = new CookieAuthService(new FakeHttpContextAccessor { HttpContext = context }, Microsoft.Extensions.Options.Options.Create(Options));

        sut.GetRefreshTokenFromCookie().ShouldBeNull();
    }

    [Fact]
    public void ClearAuthCookies_AppendsExpiredCookies_ForBothNames()
    {
        var context = new DefaultHttpContext();
        var sut = new CookieAuthService(new FakeHttpContextAccessor { HttpContext = context }, Microsoft.Extensions.Options.Options.Create(Options));

        sut.ClearAuthCookies();

        var setCookieHeaders = context.Response.Headers[HeaderNames.SetCookie];
        setCookieHeaders.Count.ShouldBe(2);
        setCookieHeaders.ShouldContain(h => h!.StartsWith("bw_access_token="));
        setCookieHeaders.ShouldContain(h => h!.StartsWith("bw_refresh_token="));
        setCookieHeaders.ShouldAllBe(h => h!.Contains("expires="));
    }

    private class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
