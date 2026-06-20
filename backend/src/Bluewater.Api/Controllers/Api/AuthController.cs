using Bluewater.Core.Dto;
using Bluewater.Core.Services;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = Bluewater.Core.Dto.LoginRequest;

namespace Bluewater.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request)
    {
        return _authService.LoginAsync(request);
    }

    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh(RefreshRequest request)
    {
        return _authService.RefreshAsync(request);
    }

    [Authorize]
    [HttpPost("logout")]
    public Task Logout()
    {
        return _authService.LogoutAsync(_currentUser.Id);
    }

    [HttpGet("permissions")]
    public Task<List<BluePermission>> AllPermissions()
    {
        return Task.FromResult(Enum.GetValues<BluePermission>().ToList());
    }
}
