using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class UserGroupServiceTests : SqliteServiceTestBase
{
    private readonly IUserGroupService _sut;

    public UserGroupServiceTests()
    {
        _sut = GetService<IUserGroupService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoGroupsExist()
    {
        var result = await _sut.ListAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_ReturnsAllGroups_WithCategoryName()
    {
        var category = await CreateCategoryAsync();
        Db.UserGroups.Add(new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.ListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Members");
        result[0].UserGroupCategoryName.ShouldBe(category.Name);
    }

    [Fact]
    public async Task GetAsync_Throws_WhenGroupDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsGroup_AndReturnsDto()
    {
        var category = await CreateCategoryAsync();
        var request = new UpsertUserGroupRequest("Members", "Active members", category.Id);

        var result = await _sut.CreateAsync(request);

        result.Name.ShouldBe(request.Name);
        result.UserGroupCategoryId.ShouldBe(category.Id);
        (await Db.UserGroups.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCategoryDoesNotExist()
    {
        var request = new UpsertUserGroupRequest("Members", "Active members", Guid.NewGuid());

        await Should.ThrowAsync<BlueValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingGroup()
    {
        var category = await CreateCategoryAsync();
        var otherCategory = await CreateCategoryAsync("Staff");
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Old", UserGroupCategoryId = category.Id };
        Db.UserGroups.Add(group);
        await Db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(group.Id, new UpsertUserGroupRequest("Renamed", "New", otherCategory.Id));

        result.Name.ShouldBe("Renamed");
        result.UserGroupCategoryId.ShouldBe(otherCategory.Id);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenGroupDoesNotExist()
    {
        var category = await CreateCategoryAsync();

        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpsertUserGroupRequest("Renamed", "New", category.Id)));
    }

    [Fact]
    public async Task DeleteAsync_RemovesGroup()
    {
        var category = await CreateCategoryAsync();
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id };
        Db.UserGroups.Add(group);
        await Db.SaveChangesAsync();

        await _sut.DeleteAsync(group.Id);

        (await Db.UserGroups.AnyAsync(x => x.Id == group.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenGroupDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsCaseInsensitiveMatches_IncludingDuplicates()
    {
        var category = await CreateCategoryAsync();
        Db.UserGroups.AddRange(
            new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "A", UserGroupCategoryId = category.Id },
            new UserGroup { Id = Guid.NewGuid(), Name = "members", Description = "B", UserGroupCategoryId = category.Id },
            new UserGroup { Id = Guid.NewGuid(), Name = "Staff", Description = "C", UserGroupCategoryId = category.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.FindByNameAsync("MEMBERS");

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsEmpty_WhenNoMatch()
    {
        var result = await _sut.FindByNameAsync("Nonexistent");

        result.ShouldBeEmpty();
    }

    private async Task<UserGroupCategory> CreateCategoryAsync(string name = "General")
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = name, Description = $"{name} members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        return category;
    }
}
