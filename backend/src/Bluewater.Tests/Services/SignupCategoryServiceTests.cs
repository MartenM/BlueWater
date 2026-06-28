using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class SignupCategoryServiceTests : SqliteServiceTestBase
{
    private readonly ISignupCategoryService _sut;

    public SignupCategoryServiceTests()
    {
        _sut = GetService<ISignupCategoryService>();
    }

    [Fact]
    public async Task CreateAsync_ReturnsCategory()
    {
        var result = await _sut.CreateAsync(new UpsertSignupCategoryRequest("Roeien", false, 1));

        result.Title.ShouldBe("Roeien");
        result.Hidden.ShouldBeFalse();
        result.SortOrder.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCategory()
    {
        var category = await _sut.CreateAsync(new UpsertSignupCategoryRequest("Old", false, 0));

        var result = await _sut.UpdateAsync(category.Id, new UpsertSignupCategoryRequest("New", true, 5));

        result.Title.ShouldBe("New");
        result.Hidden.ShouldBeTrue();
        result.SortOrder.ShouldBe(5);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesCategory()
    {
        var category = await _sut.CreateAsync(new UpsertSignupCategoryRequest("Test", false, 0));

        await _sut.DeleteAsync(category.Id);

        var remaining = await _sut.ListAsync();
        remaining.ShouldNotContain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task GetAsync_ThrowsNotFound_WhenCategoryDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListAsync_ReturnsCategoriesOrderedBySortOrder()
    {
        await _sut.CreateAsync(new UpsertSignupCategoryRequest("B", false, 2));
        await _sut.CreateAsync(new UpsertSignupCategoryRequest("A", false, 1));

        var result = await _sut.ListAsync();

        result[0].Title.ShouldBe("A");
        result[1].Title.ShouldBe("B");
    }
}
