using Bluewater.Core.Dto.Outings;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Outings;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class OutingServiceTests : SqliteServiceTestBase
{
    private readonly IOutingService _sut;

    public OutingServiceTests()
    {
        _sut = GetService<IOutingService>();
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_PersistsOuting_AndAppearsInOverviewAndUpcoming()
    {
        var (instance, user) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = user.Id;
        CurrentUserId = user.Id;

        var outing = await _sut.CreateAsync(new UpsertOutingRequest(
            instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, "Evening practice"));

        outing.UserGroupInstanceId.ShouldBe(instance.Id);
        outing.Confirmed.ShouldBeFalse();

        var overview = await _sut.GetOverviewAsync();
        overview.ShouldHaveSingleItem();
        overview[0].Outings.ShouldContain(o => o.Id == outing.Id);

        var upcoming = await _sut.GetForInstanceAsync(instance.Id, OutingView.Upcoming, 1, 25);
        upcoming.Items.ShouldContain(o => o.Id == outing.Id);
    }

    [Fact]
    public async Task GetMyInstancesAsync_ReturnsInstancesForCurrentSeasonMembership_EvenWithoutOutings()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;

        var result = await _sut.GetMyInstancesAsync();

        result.ShouldContain(i => i.Id == instance.Id);
    }

    [Fact]
    public async Task GetInstanceHistoryAsync_GroupsByAllSeasonsMembership_AndExcludesInstancesWithoutPermission()
    {
        var currentSeason = await CreateCurrentSeasonAsync();
        var pastSeason = new BlueSeason
        {
            Id = Guid.NewGuid(),
            StartDate = new DateOnly(2024, 6, 1),
            EndDate = new DateOnly(2025, 5, 31)
        };
        Db.Seasons.Add(pastSeason);
        await Db.SaveChangesAsync();

        var member = await CreateUserAsync("history-member", "history-member@example.com");
        var (currentInstance, _) = await CreateInstanceInSeasonAsync(currentSeason.Id, member);
        var (pastInstance, _) = await CreateInstanceInSeasonAsync(pastSeason.Id, member);
        var (noPermissionInstance, _) = await CreateInstanceWithoutOutingPlannerPermissionAsync(member, currentSeason.Id);

        CurrentServiceUserId = member.Id;

        var result = await _sut.GetInstanceHistoryAsync();

        result.Select(g => g.SeasonId).ShouldBe([currentSeason.Id, pastSeason.Id]);
        result.Single(g => g.SeasonId == currentSeason.Id).Instances.ShouldContain(i => i.Id == currentInstance.Id);
        result.Single(g => g.SeasonId == pastSeason.Id).Instances.ShouldContain(i => i.Id == pastInstance.Id);
        result.SelectMany(g => g.Instances).ShouldNotContain(i => i.Id == noPermissionInstance.Id);
    }

    // -------------------------------------------------------------------------
    // BlueNotFoundException
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_Throws_WhenOutingDoesNotExist()
    {
        var (_, user) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCallerIsNotAMemberOrInvited()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var outsider = await CreateUserAsync("outsider", "outsider@example.com");
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        CurrentServiceUserId = outsider.Id;
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(outing.Id));
    }

    // -------------------------------------------------------------------------
    // Validator branches
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Throws_WhenInstanceDoesNotExist()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.CreateAsync(new UpsertOutingRequest(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), null, null, null, null, null)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenBoatTypeIsNotABoat()
    {
        var (instance, user) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = user.Id;
        var nonBoatType = await CreateEquipmentTypeAsync(rowersCount: 0, coxed: false, isBoat: false);

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, nonBoatType.Id, null, null, null)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenOutingDateEndIsBeforeOutingDate()
    {
        var (instance, user) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = user.Id;
        var date = DateTime.UtcNow.AddDays(1);

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.CreateAsync(new UpsertOutingRequest(instance.Id, date, date.AddHours(-1), null, null, null, null)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenBoatTypeAndBoatTypeDifferentBothSet()
    {
        var (instance, user) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = user.Id;
        var boatType = await CreateEquipmentTypeAsync(rowersCount: 4, coxed: false, isBoat: true);

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, boatType.Id, "Other", null, null)));
    }

    // -------------------------------------------------------------------------
    // Membership enforcement
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Throws_WhenCallerIsNotAnInstanceMember()
    {
        var (instance, _) = await CreateInstanceWithMemberAsync();
        var outsider = await CreateUserAsync("outsider", "outsider@example.com");
        CurrentServiceUserId = outsider.Id;

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenGroupLacksOutingPlannerPermission()
    {
        var (instance, member) = await CreateInstanceWithoutOutingPlannerPermissionAsync();
        CurrentServiceUserId = member.Id;

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null)));
    }

    [Fact]
    public async Task InvitedOnlyParticipant_CanSetOwnRole_ButNotEditOutingFields()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var invitee = await CreateUserAsync("invitee", "invitee@example.com");
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));
        await _sut.InviteParticipantAsync(outing.Id, new InviteParticipantRequest(invitee.Id));

        CurrentServiceUserId = invitee.Id;
        var updated = await _sut.SetParticipantRoleAsync(outing.Id, invitee.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));
        updated.Participants.Single(p => p.UserId == invitee.Id).Role.ShouldBe(OutingParticipantRole.Rower);

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.UpdateAsync(outing.Id, new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(2), null, null, null, null, null)));
    }

    // -------------------------------------------------------------------------
    // Capacity
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetParticipantRoleAsync_DowngradesToReserve_WhenRowerSeatsFull()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var boatType = await CreateEquipmentTypeAsync(rowersCount: 1, coxed: false, isBoat: true);
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, boatType.Id, null, null, null));

        var second = await CreateUserAsync("second", "second@example.com");
        await AddMemberAsync(instance.Id, second.Id);

        await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));
        var detail = await _sut.SetParticipantRoleAsync(outing.Id, second.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));

        detail.Participants.Single(p => p.UserId == member.Id).Role.ShouldBe(OutingParticipantRole.Rower);
        detail.Participants.Single(p => p.UserId == second.Id).Role.ShouldBe(OutingParticipantRole.Reserve);

        var changelog = await _sut.GetChangelogAsync(outing.Id);
        changelog.ShouldContain(e => e.Fields.Contains("capacity"));
    }

    [Fact]
    public async Task SetParticipantRoleAsync_DowngradesCoxToReserve_WhenBoatIsNotCoxed()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var boatType = await CreateEquipmentTypeAsync(rowersCount: 4, coxed: false, isBoat: true);
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, boatType.Id, null, null, null));

        var detail = await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Cox));

        detail.Participants.Single(p => p.UserId == member.Id).Role.ShouldBe(OutingParticipantRole.Reserve);
    }

    [Fact]
    public async Task UpdateAsync_DemotesExcessRowers_WhenBoatTypeShrinks()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var bigBoat = await CreateEquipmentTypeAsync(rowersCount: 2, coxed: true, isBoat: true);
        var smallBoat = await CreateEquipmentTypeAsync(rowersCount: 1, coxed: false, isBoat: true);
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, bigBoat.Id, null, null, null));

        var second = await CreateUserAsync("second", "second@example.com");
        await AddMemberAsync(instance.Id, second.Id);

        await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));
        await _sut.SetParticipantRoleAsync(outing.Id, second.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));
        await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Cox));

        var updated = await _sut.UpdateAsync(outing.Id, new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, smallBoat.Id, null, null, null));

        var rowerCount = updated.Participants.Count(p => p.Role == OutingParticipantRole.Rower);
        rowerCount.ShouldBe(1);
        updated.Participants.ShouldNotContain(p => p.Role == OutingParticipantRole.Cox);
    }

    // -------------------------------------------------------------------------
    // Date change / check-in reset
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ResetsCheckIns_AndLogsDateChanged_WhenDateChanges()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var originalDate = DateTime.UtcNow.AddMinutes(10);
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, originalDate, null, null, null, null, null));
        await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));
        await _sut.CheckInAsync(outing.Id);

        var updated = await _sut.UpdateAsync(outing.Id, new UpsertOutingRequest(instance.Id, originalDate.AddDays(1), null, null, null, null, null));

        updated.Participants.Single(p => p.UserId == member.Id).CheckedIn.ShouldBeFalse();
        var changelog = await _sut.GetChangelogAsync(outing.Id);
        changelog.ShouldContain(e => e.Type == OutingChangelogType.DateChanged);
    }

    // -------------------------------------------------------------------------
    // Confirmed-freeze / confirm / did-not-happen
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConfirmAsync_Throws_WhenOutingDateHasNotPassed()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        await Should.ThrowAsync<BlueValidationException>(() => _sut.ConfirmAsync(outing.Id));
    }

    [Fact]
    public async Task MarkDidNotHappenAsync_Throws_WhenOutingDateHasNotPassed()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        await Should.ThrowAsync<BlueValidationException>(() => _sut.MarkDidNotHappenAsync(outing.Id));
    }

    [Fact]
    public async Task ConfirmAsync_FreezesOuting_AndRejectsFurtherMutation()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddMinutes(-5), null, null, null, null, null));

        var confirmed = await _sut.ConfirmAsync(outing.Id);
        confirmed.Confirmed.ShouldBeTrue();

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower)));
        await Should.ThrowAsync<BlueValidationException>(() => _sut.ConfirmAsync(outing.Id));
    }

    [Fact]
    public async Task MarkDidNotHappenAsync_SoftDeletesOuting_AfterDateHasPassed()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddMinutes(-5), null, null, null, null, null));

        await _sut.MarkDidNotHappenAsync(outing.Id);

        var raw = await Db.Outings.IgnoreQueryFilters().FirstAsync(o => o.Id == outing.Id);
        raw.DeletedAt.ShouldNotBeNull();
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.GetAsync(outing.Id));
    }

    // -------------------------------------------------------------------------
    // Check-in
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CheckInAsync_Succeeds_WithinWindow()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddMinutes(-10), null, null, null, null, null));
        await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));

        var detail = await _sut.CheckInAsync(outing.Id);
        detail.Participants.Single(p => p.UserId == member.Id).CheckedIn.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckInAsync_Throws_OutsideWindow()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddHours(5), null, null, null, null, null));
        await _sut.SetParticipantRoleAsync(outing.Id, member.Id, new SetParticipantRoleRequest(OutingParticipantRole.Rower));

        await Should.ThrowAsync<BlueValidationException>(() => _sut.CheckInAsync(outing.Id));
    }

    // -------------------------------------------------------------------------
    // Invite scoping / revival
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InviteParticipantAsync_ScopesNonMemberToOneOutingOnly()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var invitee = await CreateUserAsync("invitee", "invitee@example.com");
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        await _sut.InviteParticipantAsync(outing.Id, new InviteParticipantRequest(invitee.Id));

        var isMember = await Db.UserGroupInstanceMembers.AnyAsync(m => m.UserGroupInstanceId == instance.Id && m.UserId == invitee.Id);
        isMember.ShouldBeFalse();

        CurrentServiceUserId = invitee.Id;
        var overview = await _sut.GetOverviewAsync();
        overview.SelectMany(g => g.Outings).ShouldContain(o => o.Id == outing.Id);
    }

    [Fact]
    public async Task RemoveThenReinviteParticipant_RoundTripsViaRevival()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var invitee = await CreateUserAsync("invitee", "invitee@example.com");
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        await _sut.InviteParticipantAsync(outing.Id, new InviteParticipantRequest(invitee.Id));
        await _sut.RemoveParticipantAsync(outing.Id, invitee.Id);

        var afterRemoval = await _sut.GetAsync(outing.Id);
        afterRemoval.Participants.ShouldNotContain(p => p.UserId == invitee.Id);

        var afterReinvite = await _sut.InviteParticipantAsync(outing.Id, new InviteParticipantRequest(invitee.Id));
        afterReinvite.Participants.ShouldContain(p => p.UserId == invitee.Id);
    }

    // -------------------------------------------------------------------------
    // SearchCandidatesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SearchCandidatesAsync_ReturnsInstanceMembers()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var otherMember = await CreateUserAsync("othermember", "othermember@example.com");
        await AddMemberAsync(instance.Id, otherMember.Id);
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        var candidates = await _sut.SearchCandidatesAsync(outing.Id, null);

        candidates.ShouldContain(c => c.Id == otherMember.Id);
    }

    [Fact]
    public async Task SearchCandidatesAsync_ReturnsInvitedNonMembers()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var invitee = await CreateUserAsync("invitee", "invitee@example.com");
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));
        await _sut.InviteParticipantAsync(outing.Id, new InviteParticipantRequest(invitee.Id));

        var candidates = await _sut.SearchCandidatesAsync(outing.Id, null);

        candidates.ShouldContain(c => c.Id == invitee.Id);
    }

    [Fact]
    public async Task SearchCandidatesAsync_ExcludesUnrelatedActiveMembers()
    {
        var season = await CreateCurrentSeasonAsync();
        var (instance, member) = await CreateInstanceInSeasonAsync(season.Id);
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        var (_, unrelatedMember) = await CreateInstanceInSeasonAsync(season.Id);

        var candidates = await _sut.SearchCandidatesAsync(outing.Id, null);

        candidates.ShouldNotContain(c => c.Id == unrelatedMember.Id);
    }

    [Fact]
    public async Task SearchCandidatesAsync_HonorsSearchTerm()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var alice = await CreateUserAsync("alice", "alice@example.com");
        alice.Firstname = "Alice";
        var bob = await CreateUserAsync("bob", "bob@example.com");
        bob.Firstname = "Bob";
        Db.Users.UpdateRange(alice, bob);
        await Db.SaveChangesAsync();
        await AddMemberAsync(instance.Id, alice.Id);
        await AddMemberAsync(instance.Id, bob.Id);
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        var candidates = await _sut.SearchCandidatesAsync(outing.Id, "alice");

        candidates.ShouldContain(c => c.Id == alice.Id);
        candidates.ShouldNotContain(c => c.Id == bob.Id);
    }

    [Fact]
    public async Task SearchCandidatesAsync_Throws_WhenOutingDoesNotExist()
    {
        var (_, member) = await CreateInstanceWithMemberAsync();
        CurrentServiceUserId = member.Id;

        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.SearchCandidatesAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task SearchCandidatesAsync_Throws_WhenCallerIsNotAMemberOrInvited()
    {
        var (instance, member) = await CreateInstanceWithMemberAsync();
        var outsider = await CreateUserAsync("outsider", "outsider@example.com");
        CurrentServiceUserId = member.Id;
        var outing = await _sut.CreateAsync(new UpsertOutingRequest(instance.Id, DateTime.UtcNow.AddDays(1), null, null, null, null, null));

        CurrentServiceUserId = outsider.Id;
        await Should.ThrowAsync<BlueNotFoundException>(() => _sut.SearchCandidatesAsync(outing.Id, null));
    }

    // -------------------------------------------------------------------------
    // Test helpers
    // -------------------------------------------------------------------------

    private async Task<(UserGroupInstance instance, BlueUser member)> CreateInstanceWithMemberAsync()
    {
        var season = await CreateCurrentSeasonAsync();
        return await CreateInstanceInSeasonAsync(season.Id);
    }

    private async Task<(UserGroupInstance instance, BlueUser member)> CreateInstanceInSeasonAsync(Guid seasonId, BlueUser? existingMember = null)
    {
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = $"Rowing {Guid.NewGuid():N}"[..12], Description = "Team", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = seasonId };
        var member = existingMember ?? await CreateUserAsync($"member-{Guid.NewGuid():N}"[..16], $"{Guid.NewGuid():N}@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = member.Id });
        Db.UserGroupPermissions.Add(new UserGroupPermission { UserGroupId = group.Id, Permission = BluePermission.OutingPlannerUse });
        await Db.SaveChangesAsync();

        return (instance, member);
    }

    private async Task<(UserGroupInstance instance, BlueUser member)> CreateInstanceWithoutOutingPlannerPermissionAsync(BlueUser? existingMember = null, Guid? seasonIdOverride = null)
    {
        var seasonId = seasonIdOverride ?? (await CreateCurrentSeasonAsync()).Id;
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = $"Rowing {Guid.NewGuid():N}"[..12], Description = "Team", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = seasonId };
        var member = existingMember ?? await CreateUserAsync($"member-{Guid.NewGuid():N}"[..16], $"{Guid.NewGuid():N}@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = member.Id });
        await Db.SaveChangesAsync();

        return (instance, member);
    }

    private async Task AddMemberAsync(Guid instanceId, Guid userId)
    {
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instanceId, UserId = userId });
        await Db.SaveChangesAsync();
    }

    private async Task<EquipmentType> CreateEquipmentTypeAsync(int rowersCount, bool coxed, bool isBoat)
    {
        var type = new EquipmentType
        {
            Id = Guid.NewGuid(),
            Code = Guid.NewGuid().ToString("N")[..8],
            Name = "Boat type",
            Scull = false,
            Coxed = coxed,
            RowersCount = rowersCount,
            IsBoat = isBoat,
        };
        Db.EquipmentTypes.Add(type);
        await Db.SaveChangesAsync();
        return type;
    }
}
