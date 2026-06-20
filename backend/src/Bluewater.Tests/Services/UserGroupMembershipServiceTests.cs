using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class UserGroupMembershipServiceTests : SqliteServiceTestBase
{
    private readonly IUserGroupMembershipService _sut;

    public UserGroupMembershipServiceTests()
    {
        _sut = GetService<IUserGroupMembershipService>();
    }

    [Fact]
    public async Task GetGroupsForUserAsync_ReturnsEmpty_WhenUserHasNoMemberships()
    {
        var user = await CreateUserAsync();

        var result = await _sut.GetGroupsForUserAsync(user.Id);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetGroupsForUserAsync_ReturnsGroupsTheUserBelongsTo()
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

        var result = await _sut.GetGroupsForUserAsync(user.Id);

        result.Count.ShouldBe(1);
        result[0].GroupId.ShouldBe(group.Id);
        result[0].GroupName.ShouldBe(group.Name);
        result[0].GroupCategoryName.ShouldBe(category.Name);
        result[0].SeasonDisplayName.ShouldBe(season.Name);
    }

    [Fact]
    public async Task GetGroupsForUserAsync_DoesNotReturnGroupsForOtherUsers()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var member = await CreateUserAsync("member", "member@example.com");
        var outsider = await CreateUserAsync("outsider", "outsider@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = member.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetGroupsForUserAsync(outsider.Id);

        result.ShouldBeEmpty();
    }
}
