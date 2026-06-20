using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Context;

/// <summary>
/// Exercises the audit-stamping/soft-delete behavior that <see cref="Bluewater.Infra.Context.BluewaterContext"/>
/// applies to every entity implementing IAuditable, using UserGroupCategory as a representative entity.
/// </summary>
public class BluewaterContextAuditingTests : SqliteServiceTestBase
{
    [Fact]
    public async Task Add_StampsCreatedAtAndCreatedByUserId()
    {
        var userId = Guid.NewGuid();
        CurrentUserId = userId;

        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        category.CreatedAt.ShouldNotBe(default);
        category.CreatedByUserId.ShouldBe(userId);
        category.UpdatedAt.ShouldBeNull();
        category.UpdatedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task Add_StampsCreatedByUserId_AsEmptyGuid_WhenNoCurrentUser()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        category.CreatedByUserId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task Update_StampsUpdatedAtAndUpdatedByUserId()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        CurrentUserId = userId;
        category.Description = "Updated";
        await Db.SaveChangesAsync();

        category.UpdatedAt.ShouldNotBeNull();
        category.UpdatedByUserId.ShouldBe(userId);
    }

    [Fact]
    public async Task Remove_SoftDeletes_InsteadOfRemovingRow()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        CurrentUserId = userId;
        Db.UserGroupCategories.Remove(category);
        await Db.SaveChangesAsync();

        var stillPhysicallyPresent = await Db.UserGroupCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == category.Id);

        stillPhysicallyPresent.ShouldNotBeNull();
        stillPhysicallyPresent.DeletedAt.ShouldNotBeNull();
        stillPhysicallyPresent.DeletedByUserId.ShouldBe(userId);
    }

    [Fact]
    public async Task SoftDeletedEntity_IsExcludedFromDefaultQueries()
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        Db.UserGroupCategories.Add(category);
        await Db.SaveChangesAsync();

        Db.UserGroupCategories.Remove(category);
        await Db.SaveChangesAsync();

        (await Db.UserGroupCategories.AnyAsync(x => x.Id == category.Id)).ShouldBeFalse();
    }
}
