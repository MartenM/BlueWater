using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class UserGroupInstanceServiceTests : SqliteServiceTestBase
{
    private readonly IUserGroupInstanceService _sut;

    public UserGroupInstanceServiceTests()
    {
        _sut = GetService<IUserGroupInstanceService>();
    }

    [Fact]
    public async Task CreateAsync_PersistsInstance_AndReturnsDto()
    {
        var season = await CreateCurrentSeasonAsync();
        var group = await CreateGroupAsync();

        var result = await _sut.CreateAsync(new CreateUserGroupInstanceRequest(group.Id, season.Id));

        result.UserGroupId.ShouldBe(group.Id);
        result.SeasonId.ShouldBe(season.Id);
        result.MemberUserIds.ShouldBeEmpty();
        result.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenUserGroupDoesNotExist()
    {
        var season = await CreateCurrentSeasonAsync();

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.CreateAsync(new CreateUserGroupInstanceRequest(Guid.NewGuid(), season.Id)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSeasonDoesNotExist()
    {
        var group = await CreateGroupAsync();

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.CreateAsync(new CreateUserGroupInstanceRequest(group.Id, Guid.NewGuid())));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenInstanceForGroupAndSeasonAlreadyExists()
    {
        var season = await CreateCurrentSeasonAsync();
        var group = await CreateGroupAsync();
        await _sut.CreateAsync(new CreateUserGroupInstanceRequest(group.Id, season.Id));

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.CreateAsync(new CreateUserGroupInstanceRequest(group.Id, season.Id)));
    }

    [Fact]
    public async Task GetAsync_Throws_WhenInstanceDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesInstance()
    {
        var instance = await CreateInstanceAsync();

        await _sut.DeleteAsync(instance.Id);

        (await Db.UserGroupInstances.AnyAsync(x => x.Id == instance.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task AddMemberAsync_AddsUserToInstance()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();

        await _sut.AddMemberAsync(instance.Id, user.Id);

        var dto = await _sut.GetAsync(instance.Id);
        dto.MemberUserIds.ShouldBe([user.Id]);
    }

    [Fact]
    public async Task AddMemberAsync_IsIdempotent_WhenUserAlreadyAMember()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);

        await _sut.AddMemberAsync(instance.Id, user.Id);

        var dto = await _sut.GetAsync(instance.Id);
        dto.MemberUserIds.ShouldBe([user.Id]);
    }

    [Fact]
    public async Task AddMemberAsync_Throws_WhenUserDoesNotExist()
    {
        var instance = await CreateInstanceAsync();

        await Should.ThrowAsync<BlueValidationException>(() => _sut.AddMemberAsync(instance.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AddMemberAsync_Throws_WhenInstanceDoesNotExist()
    {
        var user = await CreateUserAsync();

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.AddMemberAsync(Guid.NewGuid(), user.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesExistingMember()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);

        await _sut.RemoveMemberAsync(instance.Id, user.Id);

        var dto = await _sut.GetAsync(instance.Id);
        dto.MemberUserIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveMemberAsync_DoesNotThrow_WhenMemberDoesNotExist()
    {
        var instance = await CreateInstanceAsync();

        await _sut.RemoveMemberAsync(instance.Id, Guid.NewGuid());
    }

    [Fact]
    public async Task AssignPermissionAsync_AddsPermission()
    {
        var instance = await CreateInstanceAsync();

        await _sut.AssignPermissionAsync(instance.Id, BluePermission.ViewGroups);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Permissions.ShouldBe([BluePermission.ViewGroups]);
    }

    [Fact]
    public async Task AssignPermissionAsync_IsIdempotent_WhenAlreadyAssigned()
    {
        var instance = await CreateInstanceAsync();
        await _sut.AssignPermissionAsync(instance.Id, BluePermission.ViewGroups);

        await _sut.AssignPermissionAsync(instance.Id, BluePermission.ViewGroups);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Permissions.ShouldBe([BluePermission.ViewGroups]);
    }

    [Fact]
    public async Task RevokePermissionAsync_RemovesExistingPermission()
    {
        var instance = await CreateInstanceAsync();
        await _sut.AssignPermissionAsync(instance.Id, BluePermission.ViewGroups);

        await _sut.RevokePermissionAsync(instance.Id, BluePermission.ViewGroups);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task RevokePermissionAsync_DoesNotThrow_WhenNotAssigned()
    {
        var instance = await CreateInstanceAsync();

        await _sut.RevokePermissionAsync(instance.Id, BluePermission.ViewGroups);
    }

    private async Task<UserGroupInstance> CreateInstanceAsync()
    {
        var season = await CreateCurrentSeasonAsync();
        var group = await CreateGroupAsync();

        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        Db.UserGroupInstances.Add(instance);
        await Db.SaveChangesAsync();

        return instance;
    }

    private async Task<UserGroup> CreateGroupAsync()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id };
        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        await Db.SaveChangesAsync();

        return group;
    }
}
