using Microsoft.AspNetCore.Http;

namespace Bluewater.Infra.Options;

public class CookieAuthOptions
{
    public string AccessTokenCookieName { get; set; } = "bw_access_token";
    public string RefreshTokenCookieName { get; set; } = "bw_refresh_token";
    public bool Secure { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
    public string? Domain { get; set; } = null;
    public string Path { get; set; } = "/";
}
