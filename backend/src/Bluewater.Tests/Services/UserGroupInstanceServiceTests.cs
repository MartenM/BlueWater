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
        result.Members.ShouldBeEmpty();
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
    public async Task CreateAsync_Throws_WhenAnotherGroupWithSameNameHasInstanceInSeason()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var groupA = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "A", UserGroupCategoryId = category.Id };
        var groupB = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "B", UserGroupCategoryId = category.Id };
        Db.UserGroupCategories.Add(category);
        Db.UserGroups.AddRange(groupA, groupB);
        await Db.SaveChangesAsync();
        await _sut.CreateAsync(new CreateUserGroupInstanceRequest(groupA.Id, season.Id));

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.CreateAsync(new CreateUserGroupInstanceRequest(groupB.Id, season.Id)));
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenSameNameGroupHasInstanceInDifferentSeason()
    {
        var season = await CreateCurrentSeasonAsync();
        var otherSeason = new BlueSeason
        {
            Id = Guid.NewGuid(),
            StartDate = new DateOnly(2024, 6, 1),
            EndDate = new DateOnly(2025, 5, 31)
        };
        Db.Seasons.Add(otherSeason);
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var groupA = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "A", UserGroupCategoryId = category.Id };
        var groupB = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "B", UserGroupCategoryId = category.Id };
        Db.UserGroupCategories.Add(category);
        Db.UserGroups.AddRange(groupA, groupB);
        await Db.SaveChangesAsync();
        await _sut.CreateAsync(new CreateUserGroupInstanceRequest(groupA.Id, season.Id));

        var result = await _sut.CreateAsync(new CreateUserGroupInstanceRequest(groupB.Id, otherSeason.Id));

        result.UserGroupId.ShouldBe(groupB.Id);
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
        dto.Members.Select(m => m.UserId).ShouldBe([user.Id]);
    }

    [Fact]
    public async Task AddMemberAsync_IsIdempotent_WhenUserAlreadyAMember()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);

        await _sut.AddMemberAsync(instance.Id, user.Id);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Members.Count.ShouldBe(1);
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
        dto.Members.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveMemberAsync_DoesNotThrow_WhenMemberDoesNotExist()
    {
        var instance = await CreateInstanceAsync();

        await _sut.RemoveMemberAsync(instance.Id, Guid.NewGuid());
    }

    [Fact]
    public async Task AddMemberAsync_ReaddsSuccessfully_AfterPreviousRemove()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);
        await _sut.RemoveMemberAsync(instance.Id, user.Id);

        await _sut.AddMemberAsync(instance.Id, user.Id);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Members.Select(m => m.UserId).ShouldBe([user.Id]);

        var rows = await Db.UserGroupInstanceMembers
            .IgnoreQueryFilters()
            .Where(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id)
            .ToListAsync();
        rows.Count.ShouldBe(1);
        rows[0].DeletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task AssignMemberRoleAsync_SetsRoleOnMember()
    {
        var (instance, role) = await CreateInstanceWithRoleAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);

        await _sut.AssignMemberRoleAsync(instance.Id, user.Id, role.Id);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Members.Single(m => m.UserId == user.Id).UserGroupCategoryRoleId.ShouldBe(role.Id);
    }

    [Fact]
    public async Task AssignMemberRoleAsync_ClearsRole_WhenNullPassed()
    {
        var (instance, role) = await CreateInstanceWithRoleAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);
        await _sut.AssignMemberRoleAsync(instance.Id, user.Id, role.Id);

        await _sut.AssignMemberRoleAsync(instance.Id, user.Id, null);

        var dto = await _sut.GetAsync(instance.Id);
        dto.Members.Single(m => m.UserId == user.Id).UserGroupCategoryRoleId.ShouldBeNull();
    }

    [Fact]
    public async Task AssignMemberRoleAsync_Throws_WhenMemberNotFound()
    {
        var (instance, role) = await CreateInstanceWithRoleAsync();

        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.AssignMemberRoleAsync(instance.Id, Guid.NewGuid(), role.Id));
    }

    [Fact]
    public async Task AssignMemberRoleAsync_Throws_WhenRoleDoesNotBelongToCategory()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();
        await _sut.AddMemberAsync(instance.Id, user.Id);

        // role from a different category
        var otherCategory = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Other", Description = "" };
        var otherRole = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = otherCategory.Id, NamePlural = "Others" };
        Db.UserGroupCategories.Add(otherCategory);
        Db.UserGroupCategoryRoles.Add(otherRole);
        await Db.SaveChangesAsync();

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.AssignMemberRoleAsync(instance.Id, user.Id, otherRole.Id));
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

    private async Task<(UserGroupInstance Instance, UserGroupCategoryRole Role)> CreateInstanceWithRoleAsync()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var role = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "Roeiers", NameMasculine = "Roeier", NameFeminine = "Roeister" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "A1", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        Db.UserGroupCategories.Add(category);
        Db.UserGroupCategoryRoles.Add(role);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        await Db.SaveChangesAsync();

        return (instance, role);
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
