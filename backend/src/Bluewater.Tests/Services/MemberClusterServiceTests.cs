using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class MemberClusterServiceTests : SqliteServiceTestBase
{
    private readonly IMemberClusterService _sut;

    public MemberClusterServiceTests()
    {
        _sut = GetService<IMemberClusterService>();
    }

    [Fact]
    public async Task CreateAsync_ReturnsCluster_WithEmptyCriteria()
    {
        var result = await _sut.CreateAsync(new UpsertMemberClusterRequest("Active Members", "All active members"));

        result.Name.ShouldBe("Active Members");
        result.Description.ShouldBe("All active members");
        result.Criteria.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndDescription()
    {
        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Old Name", "Old Desc"));

        var result = await _sut.UpdateAsync(cluster.Id, new UpsertMemberClusterRequest("New Name", "New Desc"));

        result.Name.ShouldBe("New Name");
        result.Description.ShouldBe("New Desc");
    }

    [Fact]
    public async Task AddCriterionAsync_AddsCategoryRoleCriterion()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var role = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "Coaches", SortOrder = 1 };
        Db.UserGroupCategories.Add(category);
        Db.UserGroupCategoryRoles.Add(role);
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Coaches", ""));
        var criterion = await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(
            ClusterCriterionType.GroupCategory,
            category.Id,
            role.Id,
            null));

        criterion.Type.ShouldBe(ClusterCriterionType.GroupCategory);
        criterion.CategoryId.ShouldBe(category.Id);
        criterion.CategoryName.ShouldBe("Rowing");
        criterion.RoleId.ShouldBe(role.Id);
        criterion.RoleName.ShouldBe("Coaches");
    }

    [Fact]
    public async Task AddCriterionAsync_AddsGroupCriterion()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("ELJD members", ""));
        var criterion = await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(
            ClusterCriterionType.Group,
            null, null,
            group.Id));

        criterion.Type.ShouldBe(ClusterCriterionType.Group);
        criterion.GroupId.ShouldBe(group.Id);
        criterion.GroupName.ShouldBe("ELJD");
    }

    [Fact]
    public async Task RemoveCriterionAsync_RemovesCriterion()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Rowers", ""));
        var criterion = await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(
            ClusterCriterionType.GroupCategory, category.Id, null, null));

        await _sut.RemoveCriterionAsync(cluster.Id, criterion.Id);

        var updated = await _sut.GetAsync(cluster.Id);
        updated.Criteria.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveMembersAsync_ReturnsEmpty_WhenNoCriteria()
    {
        await CreateCurrentSeasonAsync();
        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Empty", ""));

        var members = await _sut.ResolveMembersAsync(cluster.Id);

        members.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveMembersAsync_WithGroupCategoryCriterion_ReturnsAllMembersOfThatCategory()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user1 = await CreateUserAsync("u1", "u1@example.com");
        var user2 = await CreateUserAsync("u2", "u2@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user1.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user2.Id });
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("All Rowers", ""));
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.GroupCategory, category.Id, null, null));

        var members = await _sut.ResolveMembersAsync(cluster.Id);

        members.Count.ShouldBe(2);
        members.ShouldContain(m => m.UserId == user1.Id);
        members.ShouldContain(m => m.UserId == user2.Id);
    }

    [Fact]
    public async Task ResolveMembersAsync_WithRoleFilter_ExcludesMembersWithoutThatRole()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var role = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = "Coaches", SortOrder = 1 };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var coach = await CreateUserAsync("coach", "coach@example.com");
        var rower = await CreateUserAsync("rower", "rower@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroupCategoryRoles.Add(role);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = coach.Id, UserGroupCategoryRoleId = role.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = rower.Id });
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Coaches only", ""));
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.GroupCategory, category.Id, role.Id, null));

        var members = await _sut.ResolveMembersAsync(cluster.Id);

        members.Count.ShouldBe(1);
        members[0].UserId.ShouldBe(coach.Id);
    }

    [Fact]
    public async Task ResolveMembersAsync_WithGroupCriterion_ReturnsOnlyMembersOfThatGroup()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var groupA = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var groupB = new UserGroup { Id = Guid.NewGuid(), Name = "Seniors", Description = "", UserGroupCategoryId = category.Id };
        var instanceA = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = groupA.Id, SeasonId = season.Id };
        var instanceB = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = groupB.Id, SeasonId = season.Id };
        var userA = await CreateUserAsync("ua", "ua@example.com");
        var userB = await CreateUserAsync("ub", "ub@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.AddRange(groupA, groupB);
        Db.UserGroupInstances.AddRange(instanceA, instanceB);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instanceA.Id, UserId = userA.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instanceB.Id, UserId = userB.Id });
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("ELJD only", ""));
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.Group, null, null, groupA.Id));

        var members = await _sut.ResolveMembersAsync(cluster.Id);

        members.Count.ShouldBe(1);
        members[0].UserId.ShouldBe(userA.Id);
    }

    [Fact]
    public async Task ResolveMembersAsync_DeduplicatesUserWhoMatchesMultipleCriteria()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user = await CreateUserAsync("u", "u@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Double match", ""));
        // Both criteria match the same user
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.GroupCategory, category.Id, null, null));
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.Group, null, null, group.Id));

        var members = await _sut.ResolveMembersAsync(cluster.Id);

        members.Count.ShouldBe(1);
        members[0].UserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task IsMemberAsync_ReturnsTrue_WhenUserMatchesCriterion()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user = await CreateUserAsync();

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Rowers", ""));
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.GroupCategory, category.Id, null, null));

        var result = await _sut.IsMemberAsync(cluster.Id, user.Id);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsMemberAsync_ReturnsFalse_WhenUserDoesNotMatchAnyCriterion()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var member = await CreateUserAsync("member", "member@example.com");
        var outsider = await CreateUserAsync("outsider", "outsider@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = member.Id });
        await Db.SaveChangesAsync();

        var cluster = await _sut.CreateAsync(new UpsertMemberClusterRequest("Rowers", ""));
        await _sut.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.GroupCategory, category.Id, null, null));

        var result = await _sut.IsMemberAsync(cluster.Id, outsider.Id);

        result.ShouldBeFalse();
    }
}
