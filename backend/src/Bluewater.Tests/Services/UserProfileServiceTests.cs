using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class UserProfileServiceTests : SqliteServiceTestBase
{
    private readonly IUserProfileService _sut;

    public UserProfileServiceTests()
    {
        _sut = GetService<IUserProfileService>();
    }

    [Fact]
    public async Task GetAsync_ReturnsNameAndEmptyGroups_WhenUserHasNoMemberships()
    {
        var user = await CreateUserAsync();
        user.Firstname = "Jane";
        user.SurnamePrefix = "van";
        user.Surname = "Doe";
        await UserManager.UpdateAsync(user);

        var result = await _sut.GetAsync(user.Id);

        result.Id.ShouldBe(user.Id);
        result.Firstname.ShouldBe("Jane");
        result.SurnamePrefix.ShouldBe("van");
        result.Surname.ShouldBe("Doe");
        result.Fullname.ShouldBe(user.Fullname);
        result.Groups.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_ReturnsGroupsTheUserBelongsTo()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user = await CreateUserAsync();

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAsync(user.Id);

        result.Groups.Count.ShouldBe(1);
        result.Groups[0].GroupId.ShouldBe(group.Id);
        result.Groups[0].GroupName.ShouldBe(group.Name);
        result.Groups[0].GroupCategoryName.ShouldBe(category.Name);
        result.Groups[0].SeasonDisplayName.ShouldBe(season.Name);
    }

    [Fact]
    public async Task GetAsync_Throws_WhenUserDoesNotExist()
    {
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }
}
