using Bluewater.Core.Dto;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Bluewater.Core.Services;

public class CookieAuthService : ICookieAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CookieAuthOptions _options;

    public CookieAuthService(IHttpContextAccessor httpContextAccessor, IOptions<CookieAuthOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public void SetAuthCookies(AuthResponse response)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var cookieOptions = BuildCookieOptions();
        context.Response.Cookies.Append(_options.AccessTokenCookieName, response.AccessToken, cookieOptions);
        context.Response.Cookies.Append(_options.RefreshTokenCookieName, response.RefreshToken, cookieOptions);
    }

    public void ClearAuthCookies()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var deleteOptions = new CookieOptions { Path = _options.Path, Domain = _options.Domain };
        context.Response.Cookies.Delete(_options.AccessTokenCookieName, deleteOptions);
        context.Response.Cookies.Delete(_options.RefreshTokenCookieName, deleteOptions);
    }

    public string? GetRefreshTokenFromCookie() =>
        _httpContextAccessor.HttpContext?.Request.Cookies[_options.RefreshTokenCookieName];

    private CookieOptions BuildCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = _options.Secure,
        SameSite = _options.SameSite,
        Domain = _options.Domain,
        Path = _options.Path
    };
}
