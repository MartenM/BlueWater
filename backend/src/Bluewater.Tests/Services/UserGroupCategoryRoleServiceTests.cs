using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class UserGroupCategoryRoleServiceTests : SqliteServiceTestBase
{
    private readonly IUserGroupCategoryRoleService _sut;

    public UserGroupCategoryRoleServiceTests()
    {
        _sut = GetService<IUserGroupCategoryRoleService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsRolesForCategory()
    {
        var category = await CreateCategoryAsync();
        Db.UserGroupCategoryRoles.Add(new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "Roeiers", NameMasculine = "Roeier", NameFeminine = "Roeister" });
        await Db.SaveChangesAsync();

        var result = await _sut.ListAsync(category.Id);

        result.Count.ShouldBe(1);
        result[0].NamePlural.ShouldBe("Roeiers");
        result[0].NameMasculine.ShouldBe("Roeier");
        result[0].NameFeminine.ShouldBe("Roeister");
    }

    [Fact]
    public async Task ListAsync_Throws_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.ListAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAsync_Throws_WhenRoleDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsRole_AndReturnsDto()
    {
        var category = await CreateCategoryAsync();

        var result = await _sut.CreateAsync(category.Id, new UpsertUserGroupCategoryRoleRequest("Coaches", null, null));

        result.NamePlural.ShouldBe("Coaches");
        result.NameMasculine.ShouldBeNull();
        result.NameFeminine.ShouldBeNull();
        result.UserGroupCategoryId.ShouldBe(category.Id);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.CreateAsync(Guid.NewGuid(), new UpsertUserGroupCategoryRoleRequest("Roeiers", null, null)));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRole()
    {
        var category = await CreateCategoryAsync();
        var role = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "Old" };
        Db.UserGroupCategoryRoles.Add(role);
        await Db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(role.Id, new UpsertUserGroupCategoryRoleRequest("Roeiers", "Roeier", "Roeister"));

        result.NamePlural.ShouldBe("Roeiers");
        result.NameMasculine.ShouldBe("Roeier");
        result.NameFeminine.ShouldBe("Roeister");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenRoleDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpsertUserGroupCategoryRoleRequest("New", null, null)));
    }

    [Fact]
    public async Task DeleteAsync_RemovesRole()
    {
        var category = await CreateCategoryAsync();
        var role = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "Roeiers" };
        Db.UserGroupCategoryRoles.Add(role);
        await Db.SaveChangesAsync();

        await _sut.DeleteAsync(role.Id);

        (await _sut.ListAsync(category.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenRoleDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReorderAsync_UpdatesSortOrder()
    {
        var category = await CreateCategoryAsync();
        var role1 = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "A", SortOrder = 0 };
        var role2 = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "B", SortOrder = 1 };
        Db.UserGroupCategoryRoles.AddRange(role1, role2);
        await Db.SaveChangesAsync();

        await _sut.ReorderAsync(category.Id, new ReorderRolesRequest([role2.Id, role1.Id]));

        var result = await _sut.ListAsync(category.Id);
        result[0].NamePlural.ShouldBe("B");
        result[1].NamePlural.ShouldBe("A");
    }

    [Fact]
    public async Task ReorderAsync_Throws_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.ReorderAsync(Guid.NewGuid(), new ReorderRolesRequest([Guid.NewGuid()])));
    }

    private async Task<UserGroupCategory> CreateCategoryAsync()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "Rowing groups" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();
        return category;
    }
}
