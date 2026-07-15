using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Domain.Models.Groups;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class MailingTargetResolverServiceTests : SqliteServiceTestBase
{
    private readonly IMailingTargetResolverService _sut;
    private readonly IMailingService _mailingService;
    private readonly IMemberClusterService _clusterService;

    public MailingTargetResolverServiceTests()
    {
        _sut = GetService<IMailingTargetResolverService>();
        _mailingService = GetService<IMailingService>();
        _clusterService = GetService<IMemberClusterService>();
    }

    [Fact]
    public async Task ResolveRecipientsAsync_ReturnsEmpty_WhenNoTargets()
    {
        var mailing = await _mailingService.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));

        var result = await _sut.ResolveRecipientsAsync(mailing.Id);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveRecipientsAsync_UnionsClusterAndGroupInstanceMembers_Deduplicated()
    {
        var season = await CreateCurrentSeasonAsync();
        var clusterCategory = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var clusterGroup = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = clusterCategory.Id };
        var clusterInstance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = clusterGroup.Id, SeasonId = season.Id };

        var directCategory = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Coaching", Description = "" };
        var directGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Coaches", Description = "", UserGroupCategoryId = directCategory.Id };
        var directInstance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = directGroup.Id, SeasonId = season.Id };

        var clusterOnlyUser = await CreateUserAsync("cluster-user", "cluster-user@example.com");
        var directOnlyUser = await CreateUserAsync("direct-user", "direct-user@example.com");
        var sharedUser = await CreateUserAsync("shared-user", "shared-user@example.com");

        Db.UserGroupCategories.AddRange(clusterCategory, directCategory);
        Db.UserGroups.AddRange(clusterGroup, directGroup);
        Db.UserGroupInstances.AddRange(clusterInstance, directInstance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = clusterInstance.Id, UserId = clusterOnlyUser.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = clusterInstance.Id, UserId = sharedUser.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = directInstance.Id, UserId = directOnlyUser.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = directInstance.Id, UserId = sharedUser.Id });
        await Db.SaveChangesAsync();

        var cluster = await _clusterService.CreateAsync(new UpsertMemberClusterRequest("Rowers", ""));
        await _clusterService.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.GroupCategory, clusterCategory.Id, null, null));

        var mailing = await _mailingService.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));
        await _mailingService.AddTargetClusterAsync(mailing.Id, cluster.Id);
        await _mailingService.AddTargetGroupInstanceAsync(mailing.Id, directInstance.Id);

        var result = await _sut.ResolveRecipientsAsync(mailing.Id);

        result.Count.ShouldBe(3);
        result.ShouldContain(m => m.UserId == clusterOnlyUser.Id);
        result.ShouldContain(m => m.UserId == directOnlyUser.Id);
        result.ShouldContain(m => m.UserId == sharedUser.Id);
    }
}
