using System.Security.Cryptography;
using Bluewater.Core.Services;
using Bluewater.Tests.TestSupport;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using LoginRequest = Bluewater.Core.Dto.LoginRequest;

namespace Bluewater.Tests.Services;

public class AuthServiceTests : SqliteServiceTestBase
{
    private const string Password = "Test123!";

    private readonly IAuthService _sut;

    public AuthServiceTests()
    {
        _sut = GetService<IAuthService>();
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenEmailDoesNotExist()
    {
        await CreateCurrentSeasonAsync();

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("nobody@example.com", Password)));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordIsWrong()
    {
        await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync(email: "user@example.com");

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest(user.Email!, "wrong-password")));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_AndPersistsHashedRefreshToken()
    {
        await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync(email: "user@example.com");

        var result = await _sut.LoginAsync(new LoginRequest(user.Email!, Password));

        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBeNullOrWhiteSpace();

        var storedHash = Hash(result.RefreshToken);
        (await Db.RefreshTokens.AnyAsync(x => x.UserId == user.Id && x.TokenHash == storedHash)).ShouldBeTrue();
    }

    [Fact]
    public async Task LoginAsync_AccessToken_ContainsUserClaims()
    {
        await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync(email: "user@example.com");

        var result = await _sut.LoginAsync(new LoginRequest(user.Email!, Password));

        var token = new JsonWebTokenHandler().ReadJsonWebToken(result.AccessToken);
        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(user.Email);
    }

    [Fact]
    public async Task RefreshAsync_Throws_WhenTokenIsUnknown()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _sut.RefreshAsync(new RefreshRequest { RefreshToken = "not-a-real-token" }));
    }

    [Fact]
    public async Task RefreshAsync_Throws_WhenTokenIsRevoked()
    {
        await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync(email: "user@example.com");
        var login = await _sut.LoginAsync(new LoginRequest(user.Email!, Password));
        await _sut.LogoutAsync(user.Id);

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _sut.RefreshAsync(new RefreshRequest { RefreshToken = login.RefreshToken }));
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_AndReturnsNewTokens()
    {
        await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync(email: "user@example.com");
        var login = await _sut.LoginAsync(new LoginRequest(user.Email!, Password));
        var oldHash = Hash(login.RefreshToken);

        var refreshed = await _sut.RefreshAsync(new RefreshRequest { RefreshToken = login.RefreshToken });

        refreshed.RefreshToken.ShouldNotBe(login.RefreshToken);

        var oldEntry = await Db.RefreshTokens.SingleAsync(x => x.TokenHash == oldHash);
        oldEntry.RevokedAt.ShouldNotBeNull();
        oldEntry.ReplacedByTokenHash.ShouldBe(Hash(refreshed.RefreshToken));

        (await Db.RefreshTokens.AnyAsync(x => x.TokenHash == Hash(refreshed.RefreshToken) && x.RevokedAt == null)).ShouldBeTrue();
    }

    [Fact]
    public async Task LogoutAsync_RevokesAllActiveRefreshTokensForUser()
    {
        await CreateCurrentSeasonAsync();
        var user = await CreateUserAsync(email: "user@example.com");
        await _sut.LoginAsync(new LoginRequest(user.Email!, Password));
        await _sut.LoginAsync(new LoginRequest(user.Email!, Password));

        await _sut.LogoutAsync(user.Id);

        (await Db.RefreshTokens.Where(x => x.UserId == user.Id).AllAsync(x => x.RevokedAt != null)).ShouldBeTrue();
    }

    private static string Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
