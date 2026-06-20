using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Context;

/// <summary>
/// Exercises BluewaterContext's revive-on-add behavior for IAuditableRelation entities directly
/// against the DbContext (bypassing UserGroupInstanceService), using UserGroupInstanceMember as
/// a representative composite-key relation entity.
/// </summary>
public class BluewaterContextRelationRevivalTests : SqliteServiceTestBase
{
    [Fact]
    public async Task Add_RevivesSoftDeletedRow_InsteadOfInsertingDuplicate()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();

        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        Db.UserGroupInstanceMembers.Remove(
            await Db.UserGroupInstanceMembers.FirstAsync(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id));
        await Db.SaveChangesAsync();

        // Re-add under the same composite key; must not throw a primary key violation.
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var rows = await Db.UserGroupInstanceMembers
            .IgnoreQueryFilters()
            .Where(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id)
            .ToListAsync();

        rows.Count.ShouldBe(1);
        rows[0].DeletedAt.ShouldBeNull();
        rows[0].DeletedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task Add_PreservesOriginalCreatedAt_AndStampsUpdatedAt_OnRevival()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();

        var creatorId = Guid.NewGuid();
        CurrentUserId = creatorId;
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();
        var originalCreatedAt = (await Db.UserGroupInstanceMembers
            .FirstAsync(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id)).CreatedAt;

        Db.UserGroupInstanceMembers.Remove(
            await Db.UserGroupInstanceMembers.FirstAsync(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id));
        await Db.SaveChangesAsync();

        var reviverId = Guid.NewGuid();
        CurrentUserId = reviverId;
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var revived = await Db.UserGroupInstanceMembers
            .FirstAsync(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id);

        revived.CreatedAt.ShouldBe(originalCreatedAt);
        revived.CreatedByUserId.ShouldBe(creatorId);
        revived.UpdatedAt.ShouldNotBeNull();
        revived.UpdatedByUserId.ShouldBe(reviverId);
    }

    [Fact]
    public async Task Add_StampsCreatedAt_WhenNoPriorSoftDeletedRow()
    {
        var instance = await CreateInstanceAsync();
        var user = await CreateUserAsync();

        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var member = await Db.UserGroupInstanceMembers
            .FirstAsync(x => x.UserGroupInstanceId == instance.Id && x.UserId == user.Id);

        member.CreatedAt.ShouldNotBe(default);
        member.DeletedAt.ShouldBeNull();
    }

    private async Task<UserGroupInstance> CreateInstanceAsync()
    {
        var season = await CreateCurrentSeasonAsync();

        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Members", Description = "Active members", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        await Db.SaveChangesAsync();

        return instance;
    }
}
