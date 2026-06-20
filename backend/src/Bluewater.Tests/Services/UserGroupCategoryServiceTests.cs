using Bluewater.Core.Dto.Groups;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class UserGroupCategoryServiceTests : SqliteServiceTestBase
{
    private readonly IUserGroupCategoryService _sut;

    public UserGroupCategoryServiceTests()
    {
        _sut = GetService<IUserGroupCategoryService>();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoCategoriesExist()
    {
        var result = await _sut.ListAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_ReturnsAllCategories()
    {
        Db.UserGroupCategories.AddRange(
            new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" },
            new UserGroupCategory { Id = Guid.NewGuid(), Name = "Staff", Description = "Staff members" });
        await Db.SaveChangesAsync();

        var result = await _sut.ListAsync();

        result.Count.ShouldBe(2);
        result.Select(x => x.Name).ShouldBe(["General", "Staff"], ignoreOrder: true);
    }

    [Fact]
    public async Task GetAsync_ReturnsCategory_WhenItExists()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        var result = await _sut.GetAsync(category.Id);

        result.ShouldBe(new UserGroupCategoryDto(category.Id, category.Name, category.Description));
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_PersistsCategory_AndReturnsDto()
    {
        var request = new UpsertUserGroupCategoryRequest("General", "General members");

        var result = await _sut.CreateAsync(request);

        result.Name.ShouldBe(request.Name);
        result.Description.ShouldBe(request.Description);
        (await Db.UserGroupCategories.CountAsync(x => x.Id == result.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingCategory()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "Old" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(category.Id, new UpsertUserGroupCategoryRequest("Renamed", "New"));

        result.Name.ShouldBe("Renamed");
        result.Description.ShouldBe("New");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpsertUserGroupCategoryRequest("Renamed", "New")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        await _sut.DeleteAsync(category.Id);

        (await Db.UserGroupCategories.AnyAsync(x => x.Id == category.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }
}
