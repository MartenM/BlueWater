using System.Security.Cryptography;
using Bluewater.Core.Dto;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Auth;
using Bluewater.Domain.Models.Groups;
using Bluewater.Infra.Context;
using Bluewater.Infra.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using LoginRequest = Bluewater.Core.Dto.LoginRequest;

namespace Bluewater.Core.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<BlueUser> _userManager;
    private readonly BluewaterContext _db;
    private readonly TokenService _tokenService;
    private readonly ICookieAuthService _cookieAuthService;

    public AuthService(
        UserManager<BlueUser> userManager,
        BluewaterContext db,
        TokenService tokenService,
        ICookieAuthService cookieAuthService)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _cookieAuthService = cookieAuthService;
    }

    // ---------------------------
    // LOGIN
    // ---------------------------
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!valid)
            throw new UnauthorizedAccessException("Invalid credentials");

        var permissions = await GetCurrentPermissionsAsync(user.Id);
        var accessToken = _tokenService.CreateAccessToken(user, permissions);

        var refreshToken = await CreateRefreshToken(user.Id);

        var response = new AuthResponse(accessToken, refreshToken);
        _cookieAuthService.SetAuthCookies(response);
        return response;
    }

    // ---------------------------
    // REFRESH
    // ---------------------------
    public async Task<AuthResponse> RefreshAsync(RefreshRequest? request)
    {
        var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? request!.RefreshToken
            : _cookieAuthService.GetRefreshTokenFromCookie();

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("No refresh token provided");

        var tokenHash = Hash(refreshToken);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (stored == null ||
            stored.ExpiresAt < DateTime.UtcNow ||
            stored.RevokedAt != null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());

        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        // rotate refresh token
        stored.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = await CreateRefreshToken(user.Id);
        stored.ReplacedByTokenHash = Hash(newRefreshToken);

        var permissions = await GetCurrentPermissionsAsync(user.Id);
        var accessToken = _tokenService.CreateAccessToken(user, permissions);

        await _db.SaveChangesAsync();

        var response = new AuthResponse(accessToken, newRefreshToken);
        _cookieAuthService.SetAuthCookies(response);
        return response;
    }

    // ---------------------------
    // LOGOUT (revoke all tokens)
    // ---------------------------
    public async Task LogoutAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _cookieAuthService.ClearAuthCookies();
    }

    // ---------------------------
    // Helpers
    // ---------------------------
    private async Task<List<BluePermission>> GetCurrentPermissionsAsync(Guid userId)
    {
        var currentSeasonId = await _db.AppSettings
            .Select(x => x.CurrentSeasonId)
            .FirstAsync();

        var memberRoles = await _db.UserGroupInstanceMembers
            .Where(m => m.UserId == userId && m.UserGroupInstance.SeasonId == currentSeasonId)
            .Select(m => new { m.UserGroupInstance.UserGroupId, m.UserGroupCategoryRoleId })
            .ToListAsync();

        if (memberRoles.Count == 0)
            return [];

        var groupIds = memberRoles.Select(m => m.UserGroupId).Distinct().ToList();

        var groupPermissions = await _db.UserGroupPermissions
            .Where(p => groupIds.Contains(p.UserGroupId))
            .Select(p => new { p.UserGroupId, p.Permission, p.UserGroupCategoryRoleId })
            .ToListAsync();

        return memberRoles
            .SelectMany(m => groupPermissions
                .Where(p => p.UserGroupId == m.UserGroupId
                         && (p.UserGroupCategoryRoleId == null
                             || p.UserGroupCategoryRoleId == m.UserGroupCategoryRoleId))
                .Select(p => p.Permission))
            .Distinct()
            .ToList();
    }

    private async Task<string> CreateRefreshToken(Guid userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        return rawToken;
    }

    private static string Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}