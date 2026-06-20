using System.Security.Cryptography;
using Bluewater.Core.Dto;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Auth;
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

    public AuthService(
        UserManager<BlueUser> userManager,
        BluewaterContext db,
        TokenService tokenService)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
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

        var accessToken = _tokenService.CreateAccessToken(user);

        var refreshToken = await CreateRefreshToken(user.Id);

        return new AuthResponse(accessToken, refreshToken);
    }

    // ---------------------------
    // REFRESH
    // ---------------------------
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
    {
        var tokenHash = Hash(request.RefreshToken);

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

        var accessToken = _tokenService.CreateAccessToken(user);

        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, newRefreshToken);
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
    }

    // ---------------------------
    // Helpers
    // ---------------------------
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