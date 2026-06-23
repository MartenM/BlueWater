using Bluewater.Core.Dto;
using Microsoft.AspNetCore.Identity.Data;
using LoginRequest = Bluewater.Core.Dto.LoginRequest;

namespace Bluewater.Core.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(RefreshRequest? request);
    Task LogoutAsync(Guid userId);
}