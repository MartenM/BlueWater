using Bluewater.Core.Dto.Clusters;
using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Mail;
using Bluewater.Tests.TestSupport;

namespace Bluewater.Tests.Services;

public class MailingServiceTests : SqliteServiceTestBase
{
    private readonly IMailingService _sut;
    private readonly IMemberClusterService _clusterService;

    public MailingServiceTests()
    {
        _sut = GetService<IMailingService>();
        _clusterService = GetService<IMemberClusterService>();
    }

    [Fact]
    public async Task CreateAsync_ReturnsDraftMailing()
    {
        var result = await _sut.CreateAsync(new UpsertMailingRequest("Subject", "Body **markdown**", "default", null, null));

        result.Subject.ShouldBe("Subject");
        result.Status.ShouldBe(MailingStatus.Draft);
        result.ProofSendCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhileDraft()
    {
        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("Old", "Old body", "default", null, null));

        var result = await _sut.UpdateAsync(mailing.Id, new UpsertMailingRequest("New", "New body", "default", null, null));

        result.Subject.ShouldBe("New");
        result.BodyMarkdown.ShouldBe("New body");
    }

    [Fact]
    public async Task UpdateAsync_Throws_OnceSent()
    {
        await CreateCurrentSeasonAsync();
        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));
        await _sut.SendAsync(mailing.Id);

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.UpdateAsync(mailing.Id, new UpsertMailingRequest("New", "New", "default", null, null)));
    }

    [Fact]
    public async Task DeleteAsync_RemovesMailing_WhileDraft()
    {
        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));

        await _sut.DeleteAsync(mailing.Id);

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(mailing.Id));
    }

    [Fact]
    public async Task DeleteAsync_Throws_OnceSent()
    {
        await CreateCurrentSeasonAsync();
        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));
        await _sut.SendAsync(mailing.Id);

        await Should.ThrowAsync<BlueValidationException>(() => _sut.DeleteAsync(mailing.Id));
    }

    [Fact]
    public async Task AddTargetClusterAsync_ThenRemove_UpdatesTargets()
    {
        var cluster = await _clusterService.CreateAsync(new UpsertMemberClusterRequest("C", ""));
        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));

        var withTarget = await _sut.AddTargetClusterAsync(mailing.Id, cluster.Id);
        withTarget.TargetClusters.Count.ShouldBe(1);
        withTarget.TargetClusters[0].MemberClusterId.ShouldBe(cluster.Id);

        await _sut.RemoveTargetClusterAsync(mailing.Id, cluster.Id);
        var afterRemove = await _sut.GetAsync(mailing.Id);
        afterRemove.TargetClusters.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendProofAsync_IncrementsProofSendCount_AndEnqueuesJob()
    {
        CurrentUserId = (await CreateUserAsync("admin", "admin@example.com")).Id;

        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("Hi {{FirstName}}", "Body **{{FullName}}**", "default", null, null));

        await _sut.SendProofAsync(mailing.Id);

        var updated = await _sut.GetAsync(mailing.Id);
        updated.ProofSendCount.ShouldBe(1);
        BackgroundJobClient.EnqueuedJobs.Count.ShouldBe(1);
        BackgroundJobClient.EnqueuedJobs[0].Args[1].ShouldBe("admin@example.com");
        BackgroundJobClient.EnqueuedJobs[0].Args[2].ShouldBe("Hi Jane");
    }

    [Fact]
    public async Task SendAsync_ResolvesRecipients_RendersContent_AndMarksSent()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user = await CreateUserAsync("member", "member@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("Hi {{FirstName}}", "Body **{{FullName}}** <a href=\"https://example.com\">link</a>", "default", null, null));
        await _sut.AddTargetGroupInstanceAsync(mailing.Id, instance.Id);

        await _sut.SendAsync(mailing.Id);

        var updated = await _sut.GetAsync(mailing.Id);
        updated.Status.ShouldBe(MailingStatus.Sent);
        updated.SentAt.ShouldNotBeNull();
        updated.TargetGroupInstances[0].LastSentAt.ShouldNotBeNull();

        var recipient = Db.MailingRecipients.Single(x => x.MailingId == mailing.Id);
        recipient.Sent.ShouldBeFalse(); // marked Sent by the job (not run here), not by SendAsync itself
        recipient.Email.ShouldBe("member@example.com");
        recipient.RenderedSubject.ShouldBe("Hi " + user.Firstname);
        recipient.RenderedHtmlBody.ShouldContain("/api/mail/r/");
        recipient.RenderedHtmlBody.ShouldContain("/api/mail/p/" + recipient.TrackingToken + ".gif");

        BackgroundJobClient.EnqueuedJobs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_ReRun_OnlySendsToNewlyAddedRecipients()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user1 = await CreateUserAsync("member1", "member1@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user1.Id });
        await Db.SaveChangesAsync();

        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));
        await _sut.AddTargetGroupInstanceAsync(mailing.Id, instance.Id);
        await _sut.SendAsync(mailing.Id);
        BackgroundJobClient.EnqueuedJobs.Count.ShouldBe(1);

        // simulate the first recipient's send job having completed
        var firstRecipient = Db.MailingRecipients.Single(x => x.MailingId == mailing.Id);
        firstRecipient.Sent = true;
        firstRecipient.SentAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        var user2 = await CreateUserAsync("member2", "member2@example.com");
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user2.Id });
        await Db.SaveChangesAsync();

        await _sut.SendAsync(mailing.Id);

        Db.MailingRecipients.Count(x => x.MailingId == mailing.Id).ShouldBe(2);
        BackgroundJobClient.EnqueuedJobs.Count.ShouldBe(2); // no re-enqueue for the already-sent recipient
    }

    [Fact]
    public async Task SendAsync_SkipsRecipientsWithBlankEmail()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user = await CreateUserAsync("no-email", "no-email@example.com");
        user.Email = null;

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user.Id });
        await Db.SaveChangesAsync();

        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));
        await _sut.AddTargetGroupInstanceAsync(mailing.Id, instance.Id);

        await _sut.SendAsync(mailing.Id);

        Db.MailingRecipients.Count(x => x.MailingId == mailing.Id).ShouldBe(0);
        BackgroundJobClient.EnqueuedJobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsSentAndOpenedCounts()
    {
        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));

        Db.MailingRecipients.Add(new MailingRecipient
        {
            Id = Guid.NewGuid(), MailingId = mailing.Id, Email = "a@example.com", FullName = "A",
            Sent = true, Opened = true, TrackingToken = "tok-a",
        });
        Db.MailingRecipients.Add(new MailingRecipient
        {
            Id = Guid.NewGuid(), MailingId = mailing.Id, Email = "b@example.com", FullName = "B",
            Sent = true, Opened = false, TrackingToken = "tok-b",
        });
        await Db.SaveChangesAsync();

        var stats = await _sut.GetStatsAsync(mailing.Id);

        stats.SentCount.ShouldBe(2);
        stats.OpenedCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetResolvedTargetCountAsync_ReturnsDedupedCount_AcrossClusterAndGroupInstance()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "Rowing", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "ELJD", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var user1 = await CreateUserAsync("member1", "member1@example.com");
        var user2 = await CreateUserAsync("member2", "member2@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user1.Id });
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = user2.Id });
        await Db.SaveChangesAsync();

        // A cluster resolving the same underlying group, targeted alongside the group instance
        // directly — both resolve to the same two members, so the count must dedupe to 2.
        var cluster = await _clusterService.CreateAsync(new UpsertMemberClusterRequest("C", ""));
        await _clusterService.AddCriterionAsync(cluster.Id, new AddClusterCriterionRequest(ClusterCriterionType.Group, null, null, group.Id));

        var mailing = await _sut.CreateAsync(new UpsertMailingRequest("S", "B", "default", null, null));
        await _sut.AddTargetClusterAsync(mailing.Id, cluster.Id);
        await _sut.AddTargetGroupInstanceAsync(mailing.Id, instance.Id);

        var count = await _sut.GetResolvedTargetCountAsync(mailing.Id);

        count.ShouldBe(2);
    }
}
