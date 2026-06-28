using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Signup;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class SignupServiceTests : SqliteServiceTestBase
{
    private readonly ISignupService _sut;

    public SignupServiceTests()
    {
        _sut = GetService<ISignupService>();
    }

    // -------------------------------------------------------------------------
    // Admin CRUD
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AdminCreateAsync_ReturnsSignupWithClusters()
    {
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var _actorId = Guid.NewGuid();
        CurrentServiceUserId = _actorId;
        CurrentUserId = _actorId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var result = await _sut.AdminCreateAsync(new UpsertSignupRequest(
            "Test Signup", null, cat.Id, null,
            AllowMultiple: false, AllowDelete: true, AllowUpdate: true,
            null, null, false, false,
            [cluster.Id]));

        result.Title.ShouldBe("Test Signup");
        result.ClusterIds.ShouldContain(cluster.Id);
        result.Fields.ShouldBeEmpty();
        result.Responses.ShouldBeEmpty();
    }

    [Fact]
    public async Task AdminUpdateAsync_ReplacesClusterAssignments()
    {
        var (clusterA, _) = await CreateClusterAsync();
        var (clusterB, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var _actorId = Guid.NewGuid();
        CurrentServiceUserId = _actorId;
        CurrentUserId = _actorId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var signup = await _sut.AdminCreateAsync(CreateUpsertRequest([clusterA.Id], cat.Id));
        var updated = await _sut.AdminUpdateAsync(signup.Id, CreateUpsertRequest([clusterB.Id], cat.Id));

        updated.ClusterIds.ShouldNotContain(clusterA.Id);
        updated.ClusterIds.ShouldContain(clusterB.Id);
    }

    [Fact]
    public async Task AdminDeleteAsync_SoftDeletesSignup()
    {
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var signup = await _sut.AdminCreateAsync(CreateUpsertRequest([cluster.Id], cat.Id));
        await _sut.AdminDeleteAsync(signup.Id);

        var list = await _sut.AdminListAsync();
        list.ShouldNotContain(s => s.Id == signup.Id);
    }

    [Fact]
    public async Task AdminDeleteAsync_ThrowsValidation_WhenNotOwnerWithoutModifyOthers()
    {
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var ownerId = Guid.NewGuid();
        CurrentServiceUserId = ownerId;
        CurrentUserId = ownerId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var signup = await _sut.AdminCreateAsync(CreateUpsertRequest([cluster.Id], cat.Id));

        // Switch to a different user without AdminSignupModifyOthers
        var _actorId = Guid.NewGuid();
        CurrentServiceUserId = _actorId;
        CurrentUserId = _actorId;

        await Should.ThrowAsync<BlueValidationException>(() => _sut.AdminDeleteAsync(signup.Id));
    }

    [Fact]
    public async Task AdminAddFieldAsync_AddsFieldToSignup()
    {
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var _actorId = Guid.NewGuid();
        CurrentServiceUserId = _actorId;
        CurrentUserId = _actorId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var signup = await _sut.AdminCreateAsync(CreateUpsertRequest([cluster.Id], cat.Id));
        var field = await _sut.AdminAddFieldAsync(signup.Id, new UpsertSignupInputFieldRequest(
            "Eet je mee?", null, SignupInputFieldType.Checkbox, null, true, 0));

        field.Title.ShouldBe("Eet je mee?");
        field.Type.ShouldBe(SignupInputFieldType.Checkbox);
        field.Visible.ShouldBeTrue();
    }

    [Fact]
    public async Task AdminReorderFieldsAsync_UpdatesSortOrder()
    {
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var _actorId = Guid.NewGuid();
        CurrentServiceUserId = _actorId;
        CurrentUserId = _actorId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var signup = await _sut.AdminCreateAsync(CreateUpsertRequest([cluster.Id], cat.Id));
        var field1 = await _sut.AdminAddFieldAsync(signup.Id, new UpsertSignupInputFieldRequest("First", null, SignupInputFieldType.Textbox, null, true, 0));
        var field2 = await _sut.AdminAddFieldAsync(signup.Id, new UpsertSignupInputFieldRequest("Second", null, SignupInputFieldType.Textbox, null, true, 1));

        await _sut.AdminReorderFieldsAsync(signup.Id, new ReorderFieldsRequest([field2.Id, field1.Id]));

        var updated = await _sut.AdminGetAsync(signup.Id);
        updated.Fields[0].Id.ShouldBe(field2.Id);
        updated.Fields[1].Id.ShouldBe(field1.Id);
    }

    // -------------------------------------------------------------------------
    // Cluster access
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ListForMemberAsync_ReturnsOnlySignupsAccessibleByUsersCluster()
    {
        var season = await CreateCurrentSeasonAsync();
        var (clusterA, _) = await CreateClusterAsync();
        var (clusterB, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var user = await CreateUserAsync();
        await AddUserToClusterAsync(user.Id, clusterA.Id, season.Id);

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;
        CurrentUserPermissions.Add(BluePermission.SignupView);

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);
        var signupA = await _sut.AdminCreateAsync(CreateUpsertRequest([clusterA.Id], cat.Id));
        var signupB = await _sut.AdminCreateAsync(CreateUpsertRequest([clusterB.Id], cat.Id));

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;
        var result = await _sut.ListForMemberAsync();

        result.ShouldContain(s => s.Id == signupA.Id);
        result.ShouldNotContain(s => s.Id == signupB.Id);
    }

    [Fact]
    public async Task ListForMemberAsync_ExcludesSignupsBeyondHideAfterDays()
    {
        var season = await CreateCurrentSeasonAsync();
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var user = await CreateUserAsync();
        await AddUserToClusterAsync(user.Id, cluster.Id, season.Id);

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        // Signup with end date 30 days ago (beyond HideAfterDays=14)
        var oldSignup = new Domain.Models.Signup.Signup
        {
            Id = Guid.NewGuid(), Title = "Old", EndDate = DateTime.UtcNow.AddDays(-30),
            CategoryId = cat.Id,
            AllowMultiple = false, AllowDelete = true, AllowUpdate = true,
            Clusters = [new SignupCluster { MemberClusterId = cluster.Id }],
            CreatedByUserId = adminId, CreatedAt = DateTime.UtcNow,
        };
        Db.Signups.Add(oldSignup);
        await Db.SaveChangesAsync();

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;
        var result = await _sut.ListForMemberAsync();

        result.ShouldNotContain(s => s.Id == oldSignup.Id);
    }

    [Fact]
    public async Task ListForMemberAsync_IncludesSignupsWithinHideAfterDays()
    {
        var season = await CreateCurrentSeasonAsync();
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var user = await CreateUserAsync();
        await AddUserToClusterAsync(user.Id, cluster.Id, season.Id);

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        // Signup with end date 5 days ago (within HideAfterDays=14)
        var recentSignup = new Domain.Models.Signup.Signup
        {
            Id = Guid.NewGuid(), Title = "Recent", EndDate = DateTime.UtcNow.AddDays(-5),
            CategoryId = cat.Id,
            AllowMultiple = false, AllowDelete = true, AllowUpdate = true,
            Clusters = [new SignupCluster { MemberClusterId = cluster.Id }],
            CreatedByUserId = adminId, CreatedAt = DateTime.UtcNow,
        };
        Db.Signups.Add(recentSignup);
        await Db.SaveChangesAsync();

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;
        var result = await _sut.ListForMemberAsync();

        result.ShouldContain(s => s.Id == recentSignup.Id);
    }

    [Fact]
    public async Task GetForMemberAsync_ThrowsNotFound_WhenUserNotInCluster()
    {
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        await CreateCurrentSeasonAsync();

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);
        var signup = await _sut.AdminCreateAsync(CreateUpsertRequest([cluster.Id], cat.Id));

        var outsider = await CreateUserAsync("outsider", "outsider@example.com");
        CurrentServiceUserId = outsider.Id;
        CurrentUserId = outsider.Id;

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetForMemberAsync(signup.Id));
    }

    // -------------------------------------------------------------------------
    // Response lifecycle
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SubmitResponseAsync_CreatesResponseWithFieldValues()
    {
        var (signup, user) = await SetupAccessibleSignupAsync();

        var result = await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        result.UserId.ShouldBe(user.Id);
        result.Status.ShouldBe("valid");
        result.Reservation.ShouldBeFalse();
    }

    [Fact]
    public async Task SubmitResponseAsync_Throws_WhenSignupClosed()
    {
        var season = await CreateCurrentSeasonAsync();
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var user = await CreateUserAsync();
        await AddUserToClusterAsync(user.Id, cluster.Id, season.Id);

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var closedSignup = new Domain.Models.Signup.Signup
        {
            Id = Guid.NewGuid(), Title = "Closed", EndDate = DateTime.UtcNow.AddDays(-1),
            CategoryId = cat.Id,
            AllowMultiple = false, AllowDelete = true, AllowUpdate = true,
            Clusters = [new SignupCluster { MemberClusterId = cluster.Id }],
            CreatedByUserId = adminId, CreatedAt = DateTime.UtcNow,
        };
        Db.Signups.Add(closedSignup);
        await Db.SaveChangesAsync();

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;
        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.SubmitResponseAsync(closedSignup.Id, new SubmitResponseRequest([])));
    }

    [Fact]
    public async Task SubmitResponseAsync_Throws_WhenAlreadyRespondedAndMultipleNotAllowed()
    {
        var (signup, _) = await SetupAccessibleSignupAsync(allowMultiple: false);

        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));
        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([])));
    }

    [Fact]
    public async Task SubmitResponseAsync_Succeeds_WhenAlreadyRespondedAndMultipleAllowed()
    {
        var (signup, _) = await SetupAccessibleSignupAsync(allowMultiple: true);

        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));
        var result = await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SubmitResponseAsync_AssignsWaitlistStatus_WhenAtMaxSignups()
    {
        var (signup, user1) = await SetupAccessibleSignupAsync(maxSignups: 1);

        // First response fills the slot
        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        // Second user goes on waitlist
        var user2 = await CreateUserAsync("u2", "u2@example.com");
        var season = await GetCurrentSeasonAsync();
        await AddUserToClusterAsync(user2.Id, (await GetSignupCluster(signup.Id)), season.Id);
        CurrentServiceUserId = user2.Id;
        CurrentUserId = user2.Id;

        var result = await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));
        result.Status.ShouldBe("waitlist");
    }

    [Fact]
    public async Task SubmitResponseAsync_Throws_WhenWaitlistAlsoFull()
    {
        var (signup, user1) = await SetupAccessibleSignupAsync(maxSignups: 1, maxWaitlist: 1);

        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        var season = await GetCurrentSeasonAsync();
        var clusterId = await GetSignupCluster(signup.Id);

        var user2 = await CreateUserAsync("u2", "u2@example.com");
        await AddUserToClusterAsync(user2.Id, clusterId, season.Id);
        CurrentServiceUserId = user2.Id;
        CurrentUserId = user2.Id;
        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        var user3 = await CreateUserAsync("u3", "u3@example.com");
        await AddUserToClusterAsync(user3.Id, clusterId, season.Id);
        CurrentServiceUserId = user3.Id;
        CurrentUserId = user3.Id;
        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([])));
    }

    [Fact]
    public async Task UpdateMyResponseAsync_Throws_WhenUpdateNotAllowed()
    {
        var (signup, _) = await SetupAccessibleSignupAsync(allowUpdate: false);
        var response = await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.UpdateMyResponseAsync(signup.Id, response.Id, new UpdateResponseRequest([])));
    }

    [Fact]
    public async Task DeleteMyResponseAsync_Throws_WhenDeleteNotAllowed()
    {
        var (signup, _) = await SetupAccessibleSignupAsync(allowDelete: false);
        var response = await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        await Should.ThrowAsync<BlueValidationException>(
            () => _sut.DeleteMyResponseAsync(signup.Id, response.Id));
    }

    // -------------------------------------------------------------------------
    // Detail / member view
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetForMemberAsync_HideSignups_ReturnsNullResponses()
    {
        var (signup, _) = await SetupAccessibleSignupAsync(hideSignups: true);
        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        var detail = await _sut.GetForMemberAsync(signup.Id);

        detail.Responses.ShouldBeNull();
    }

    [Fact]
    public async Task GetForMemberAsync_Anonymous_HidesUserIdentity()
    {
        var (signup, _) = await SetupAccessibleSignupAsync(anonymous: true);
        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        var detail = await _sut.GetForMemberAsync(signup.Id);

        detail.Responses.ShouldNotBeNull();
        detail.Responses![0].UserId.ShouldBeNull();
        detail.Responses![0].UserFullname.ShouldBeNull();
    }

    [Fact]
    public async Task GetMyResponsesAsync_IgnoresHideAfterDays()
    {
        var season = await CreateCurrentSeasonAsync();
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var user = await CreateUserAsync();
        await AddUserToClusterAsync(user.Id, cluster.Id, season.Id);

        var adminId = Guid.NewGuid();
        // Add an old signup
        var oldSignup = new Domain.Models.Signup.Signup
        {
            Id = Guid.NewGuid(), Title = "Old", EndDate = DateTime.UtcNow.AddDays(-30),
            CategoryId = cat.Id,
            AllowMultiple = false, AllowDelete = true, AllowUpdate = true,
            Clusters = [new SignupCluster { MemberClusterId = cluster.Id }],
            CreatedByUserId = adminId, CreatedAt = DateTime.UtcNow,
        };
        var response = new SignupResponse
        {
            Id = Guid.NewGuid(), SignupId = oldSignup.Id, UserId = user.Id,
            CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
        };
        Db.Signups.Add(oldSignup);
        Db.SignupResponses.Add(response);
        await Db.SaveChangesAsync();

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;
        var result = await _sut.GetMyResponsesAsync();

        result.ShouldContain(s => s.Id == oldSignup.Id);
    }

    // -------------------------------------------------------------------------
    // Admin response management
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AdminSetReservationAsync_SetsFlag()
    {
        var (signup, user) = await SetupAccessibleSignupAsync();
        var response = await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);
        CurrentUserPermissions.Add(BluePermission.AdminSignupModifyOthers);

        var updated = await _sut.AdminSetReservationAsync(signup.Id, response.Id, new SetReservationRequest(true));

        updated.Reservation.ShouldBeTrue();
        updated.Status.ShouldBe("reservation");
    }

    [Fact]
    public async Task AdminExportCsvAsync_ReturnsCsvBytes()
    {
        var (signup, _) = await SetupAccessibleSignupAsync();
        await _sut.SubmitResponseAsync(signup.Id, new SubmitResponseRequest([]));

        var adminId = signup.Id; // dummy; actual id from setup
        var _actorId = Guid.NewGuid();
        CurrentServiceUserId = _actorId;
        CurrentUserId = _actorId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);
        CurrentUserPermissions.Add(BluePermission.AdminSignupModifyOthers);

        var bytes = await _sut.AdminExportCsvAsync(signup.Id);

        bytes.ShouldNotBeEmpty();
        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        csv.ShouldContain("Naam");
        csv.ShouldContain("Status");
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<(Domain.Models.Signup.Signup Signup, Domain.Models.BlueUser User)> SetupAccessibleSignupAsync(
        bool allowMultiple = false,
        bool allowDelete = true,
        bool allowUpdate = true,
        bool hideSignups = false,
        bool anonymous = false,
        int? maxSignups = null,
        int? maxWaitlist = null)
    {
        var season = await CreateCurrentSeasonAsync();
        var (cluster, _) = await CreateClusterAsync();
        var cat = await CreateSignupCategoryAsync();
        var user = await CreateUserAsync();
        await AddUserToClusterAsync(user.Id, cluster.Id, season.Id);

        var adminId = Guid.NewGuid();
        CurrentServiceUserId = adminId;
        CurrentUserId = adminId;
        CurrentUserPermissions.Add(BluePermission.AdminSignupModify);

        var signupDto = await _sut.AdminCreateAsync(new UpsertSignupRequest(
            "Test Signup", null, cat.Id, null,
            AllowMultiple: allowMultiple,
            AllowDelete: allowDelete,
            AllowUpdate: allowUpdate,
            MaxSignups: maxSignups,
            MaxWaitlist: maxWaitlist,
            HideSignups: hideSignups,
            Anonymous: anonymous,
            ClusterIds: [cluster.Id]));

        var signup = await Db.Signups.FindAsync(signupDto.Id);

        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;

        return (signup!, user);
    }

    private async Task<(MemberCluster Cluster, Guid ClusterId)> CreateClusterAsync()
    {
        var cluster = new MemberCluster
        {
            Id = Guid.NewGuid(), Name = $"Cluster-{Guid.NewGuid():N}", Description = "",
            CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow,
        };
        Db.MemberClusters.Add(cluster);
        await Db.SaveChangesAsync();
        return (cluster, cluster.Id);
    }

    private async Task AddUserToClusterAsync(Guid userId, Guid clusterId, Guid seasonId)
    {
        // Create a group + category linked to the cluster via a GroupCategory criterion
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = $"Cat-{Guid.NewGuid():N}", Description = "" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = $"Grp-{Guid.NewGuid():N}", Description = "", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = seasonId };
        var criterion = new MemberClusterCriterion
        {
            Id = Guid.NewGuid(), MemberClusterId = clusterId,
            Type = ClusterCriterionType.GroupCategory, UserGroupCategoryId = category.Id,
        };

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new() { UserGroupInstanceId = instance.Id, UserId = userId });
        Db.MemberClusterCriteria.Add(criterion);
        await Db.SaveChangesAsync();
    }

    private async Task<BlueSeason> GetCurrentSeasonAsync()
    {
        var settings = await Db.AppSettings.FirstAsync();
        return await Db.Seasons.FindAsync(settings.CurrentSeasonId) ?? throw new InvalidOperationException("No season.");
    }

    private async Task<Guid> GetSignupCluster(Guid signupId)
    {
        var sc = await Db.SignupClusters.FirstAsync(x => x.SignupId == signupId);
        return sc.MemberClusterId;
    }

    private async Task<SignupCategory> CreateSignupCategoryAsync()
    {
        var cat = new SignupCategory
        {
            Id = Guid.NewGuid(), Title = $"Cat-{Guid.NewGuid():N}",
            CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow,
        };
        Db.SignupCategories.Add(cat);
        await Db.SaveChangesAsync();
        return cat;
    }

    private static UpsertSignupRequest CreateUpsertRequest(List<Guid> clusterIds, Guid categoryId) =>
        new("Test Signup", null, categoryId, null,
            AllowMultiple: false, AllowDelete: true, AllowUpdate: true,
            null, null, false, false, clusterIds);
}
