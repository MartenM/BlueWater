using Bluewater.Core.Dto.Users;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Tests.TestSupport;
using FluentValidation;

namespace Bluewater.Tests.Services;

public class UserServiceTests : SqliteServiceTestBase
{
    private readonly IUserService _sut;

    public UserServiceTests()
    {
        _sut = GetService<IUserService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoUsersExist()
    {
        var result = await _sut.ListAsync(1, 20, null);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_OrdersBySurnameThenFirstname_AndPaginates()
    {
        await CreateUserAsync("zuser", "z@example.com");
        await CreateUserAsync("auser", "a@example.com");
        await CreateUserAsync("muser", "m@example.com");

        var page1 = await _sut.ListAsync(1, 2, null);
        page1.Items.Count.ShouldBe(2);
        page1.TotalCount.ShouldBe(3);

        var page2 = await _sut.ListAsync(2, 2, null);
        page2.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_FiltersBySearchTerm()
    {
        await CreateUserAsync("alice", "alice@example.com");
        await CreateUserAsync("bob", "bob@example.com");

        var result = await _sut.ListAsync(1, 20, "ali");

        result.Items.Count.ShouldBe(1);
        result.Items.Single().UserName.ShouldBe("alice");
    }

    [Fact]
    public async Task GetAsync_ReturnsUser_WhenItExists()
    {
        var user = await CreateUserAsync("alice", "alice@example.com");

        var result = await _sut.GetAsync(user.Id);

        result.UserName.ShouldBe("alice");
        result.Email.ShouldBe("alice@example.com");
    }

    [Fact]
    public async Task GetAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsUser_AndReturnsGeneratedPassword()
    {
        var request = BuildCreateRequest("newuser", "newuser@example.com");

        var result = await _sut.CreateAsync(request);

        result.User.UserName.ShouldBe("newuser");
        result.User.Email.ShouldBe("newuser@example.com");
        result.GeneratedPassword.ShouldNotBeNullOrEmpty();

        var created = await _sut.GetAsync(result.User.Id);
        created.Firstname.ShouldBe("First");
        created.Address.City.ShouldBe("Groningen");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenUserNameAlreadyExists()
    {
        await CreateUserAsync("dupe", "dupe1@example.com");
        var request = BuildCreateRequest("dupe", "dupe2@example.com");

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEmailAlreadyExists()
    {
        await CreateUserAsync("dupe1", "dupe@example.com");
        var request = BuildCreateRequest("dupe2", "dupe@example.com");

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenFirstnameIsEmpty()
    {
        var request = BuildCreateRequest("newuser", "newuser@example.com") with { Firstname = "" };

        await Should.ThrowAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUser()
    {
        var user = await CreateUserAsync("original", "original@example.com");
        var request = BuildUpdateRequest("updated", "updated@example.com");

        var result = await _sut.UpdateAsync(user.Id, request);

        result.UserName.ShouldBe("updated");
        result.Email.ShouldBe("updated@example.com");
        result.Firstname.ShouldBe("First");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), BuildUpdateRequest("updated", "updated@example.com")));
    }

    [Fact]
    public async Task UpdateAsync_DoesNotThrow_WhenUserNameIsUnchanged()
    {
        var user = await CreateUserAsync("same", "same@example.com");
        var request = BuildUpdateRequest("same", "same@example.com");

        var result = await _sut.UpdateAsync(user.Id, request);

        result.UserName.ShouldBe("same");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenUserNameAlreadyUsedByAnotherUser()
    {
        await CreateUserAsync("taken", "taken@example.com");
        var user = await CreateUserAsync("mine", "mine@example.com");

        await Should.ThrowAsync<ValidationException>(
            () => _sut.UpdateAsync(user.Id, BuildUpdateRequest("taken", "mine@example.com")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var user = await CreateUserAsync("todelete", "todelete@example.com");

        await _sut.DeleteAsync(user.Id);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(user.Id));
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    private static CreateUserRequest BuildCreateRequest(string userName, string email) =>
        new(
            userName,
            email,
            "First",
            "",
            "Last",
            "0612345678",
            new BlueAddressDto("Street 1", "Groningen", "1234AB"),
            new BlueAddressDto("Street 2", "Groningen", "1234AB"),
            "0698765432",
            new DateOnly(2000, 1, 1),
            BlueUserSex.Unknown);

    private static UpdateUserRequest BuildUpdateRequest(string userName, string email) =>
        new(
            userName,
            email,
            "First",
            "",
            "Last",
            "0612345678",
            new BlueAddressDto("Street 1", "Groningen", "1234AB"),
            new BlueAddressDto("Street 2", "Groningen", "1234AB"),
            "0698765432",
            new DateOnly(2000, 1, 1),
            BlueUserSex.Unknown);
}
