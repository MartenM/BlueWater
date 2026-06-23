using Bluewater.Core.Dto;

namespace Bluewater.Core.Services.Abstractions;

public interface ICookieAuthService
{
    void SetAuthCookies(AuthResponse response);
    void ClearAuthCookies();
    string? GetRefreshTokenFromCookie();
}
