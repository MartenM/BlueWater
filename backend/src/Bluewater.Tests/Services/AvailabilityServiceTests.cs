using Bluewater.Core.Dto.Availability;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Outings;
using Bluewater.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Tests.Services;

public class AvailabilityServiceTests : SqliteServiceTestBase
{
    private readonly IAvailabilityService _sut;

    public AvailabilityServiceTests()
    {
        _sut = GetService<IAvailabilityService>();
    }

    // -------------------------------------------------------------------------
    // GetMyWeekAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyWeekAsync_ReturnsSevenDays_IncludingEmptyOnes()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;

        var monday = new DateOnly(2026, 7, 6);
        var result = await _sut.GetMyWeekAsync(monday);

        result.Days.Count.ShouldBe(7);
        result.Days.Select(d => d.Date).ShouldBe(Enumerable.Range(0, 7).Select(monday.AddDays));
        result.Days.ShouldAllBe(d => d.Blocks.Count == 0);
    }

    [Fact]
    public async Task GetMyWeekAsync_NormalizesWeekStartToMonday()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;

        var wednesday = new DateOnly(2026, 7, 8);
        var result = await _sut.GetMyWeekAsync(wednesday);

        result.WeekStart.ShouldBe(new DateOnly(2026, 7, 6));
    }

    [Fact]
    public async Task GetMyWeekAsync_ExcludesBlocksOutsideTheWeek_AndOnlyReturnsOwnBlocks()
    {
        var user = await CreateUserAsync();
        var other = await CreateUserAsync("other", "other@example.com");
        var monday = new DateOnly(2026, 7, 6);

        CurrentServiceUserId = user.Id;
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(monday, [new AvailabilityBlockInputDto(new TimeOnly(18, 0), new TimeOnly(20, 0))]));
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(monday.AddDays(7), [new AvailabilityBlockInputDto(new TimeOnly(18, 0), new TimeOnly(20, 0))]));

        CurrentServiceUserId = other.Id;
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(monday, [new AvailabilityBlockInputDto(new TimeOnly(9, 0), new TimeOnly(10, 0))]));

        CurrentServiceUserId = user.Id;
        var result = await _sut.GetMyWeekAsync(monday);

        result.Days.Single(d => d.Date == monday).Blocks.ShouldHaveSingleItem();
        result.Days.Single(d => d.Date == monday).Blocks[0].StartTime.ShouldBe(new TimeOnly(18, 0));
        result.Days.ShouldAllBe(d => d.Date == monday || d.Blocks.Count == 0);
    }

    // -------------------------------------------------------------------------
    // SetMyDayAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetMyDayAsync_InsertsBlocks_ForFreshDate()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;
        var date = new DateOnly(2026, 7, 6);

        var result = await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(date,
            [new AvailabilityBlockInputDto(new TimeOnly(6, 0), new TimeOnly(8, 0))]));

        result.ShouldHaveSingleItem();
        result[0].StartTime.ShouldBe(new TimeOnly(6, 0));
        result[0].EndTime.ShouldBe(new TimeOnly(8, 0));
    }

    [Fact]
    public async Task SetMyDayAsync_FullyReplaces_PreviousBlocksForThatDate()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;
        var date = new DateOnly(2026, 7, 6);

        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(date, [new AvailabilityBlockInputDto(new TimeOnly(6, 0), new TimeOnly(8, 0))]));
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(date, [new AvailabilityBlockInputDto(new TimeOnly(18, 0), new TimeOnly(19, 0))]));

        var raw = await Db.AvailabilityBlocks.Where(b => b.UserId == user.Id && b.Date == date).ToListAsync();
        raw.ShouldHaveSingleItem();
        raw[0].StartTime.ShouldBe(new TimeOnly(18, 0));
    }

    [Fact]
    public async Task SetMyDayAsync_DoesNotAffect_AnotherUsersBlocks()
    {
        var user = await CreateUserAsync();
        var other = await CreateUserAsync("other2", "other2@example.com");
        var date = new DateOnly(2026, 7, 6);

        CurrentServiceUserId = other.Id;
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(date, [new AvailabilityBlockInputDto(new TimeOnly(9, 0), new TimeOnly(10, 0))]));

        CurrentServiceUserId = user.Id;
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(date, [new AvailabilityBlockInputDto(new TimeOnly(6, 0), new TimeOnly(8, 0))]));

        var otherBlocks = await Db.AvailabilityBlocks.Where(b => b.UserId == other.Id && b.Date == date).ToListAsync();
        otherBlocks.ShouldHaveSingleItem();
        otherBlocks[0].StartTime.ShouldBe(new TimeOnly(9, 0));
    }

    [Fact]
    public async Task SetMyDayAsync_Throws_WhenStartIsNotBeforeEnd()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.SetMyDayAsync(new SetDayAvailabilityRequest(new DateOnly(2026, 7, 6),
                [new AvailabilityBlockInputDto(new TimeOnly(10, 0), new TimeOnly(9, 0))])));
    }

    [Fact]
    public async Task SetMyDayAsync_Throws_WhenTimeIsNotOnFifteenMinuteBoundary()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.SetMyDayAsync(new SetDayAvailabilityRequest(new DateOnly(2026, 7, 6),
                [new AvailabilityBlockInputDto(new TimeOnly(10, 5), new TimeOnly(11, 0))])));
    }

    [Fact]
    public async Task SetMyDayAsync_Throws_WhenBlocksOverlap()
    {
        var user = await CreateUserAsync();
        CurrentServiceUserId = user.Id;

        await Should.ThrowAsync<FluentValidation.ValidationException>(() =>
            _sut.SetMyDayAsync(new SetDayAvailabilityRequest(new DateOnly(2026, 7, 6),
            [
                new AvailabilityBlockInputDto(new TimeOnly(9, 0), new TimeOnly(11, 0)),
                new AvailabilityBlockInputDto(new TimeOnly(10, 0), new TimeOnly(12, 0)),
            ])));
    }

    // -------------------------------------------------------------------------
    // GetInstanceWeekAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetInstanceWeekAsync_GroupsMembersByRole()
    {
        var (instance, member, role) = await CreateInstanceWithRoleAsync("Rowers");
        CurrentServiceUserId = member.Id;

        var result = await _sut.GetInstanceWeekAsync(instance.Id, new DateOnly(2026, 7, 6));

        result.RoleGroups.ShouldHaveSingleItem();
        result.RoleGroups[0].UserGroupCategoryRoleId.ShouldBe(role.Id);
        result.RoleGroups[0].RoleLabel.ShouldBe("Rowers");
        result.RoleGroups[0].Members.ShouldContain(m => m.UserId == member.Id);
    }

    [Fact]
    public async Task GetInstanceWeekAsync_AssemblesMembersWeek_ScopedToRequestedWeek()
    {
        var (instance, member, _) = await CreateInstanceWithRoleAsync("Rowers");
        var monday = new DateOnly(2026, 7, 6);

        CurrentServiceUserId = member.Id;
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(monday, [new AvailabilityBlockInputDto(new TimeOnly(18, 0), new TimeOnly(20, 0))]));
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(monday.AddDays(7), [new AvailabilityBlockInputDto(new TimeOnly(18, 0), new TimeOnly(20, 0))]));

        var result = await _sut.GetInstanceWeekAsync(instance.Id, monday);

        var memberDays = result.RoleGroups.Single().Members.Single(m => m.UserId == member.Id).Days;
        memberDays.Count.ShouldBe(7);
        memberDays.Single(d => d.Date == monday).Blocks.ShouldHaveSingleItem();
        memberDays.ShouldAllBe(d => d.Date == monday || d.Blocks.Count == 0);
    }

    [Fact]
    public async Task GetInstanceWeekAsync_ShowsBlocksSetOutsideAnyGroupContext()
    {
        var (instance, member, _) = await CreateInstanceWithRoleAsync("Rowers");
        var monday = new DateOnly(2026, 7, 6);

        // Availability is global per user, set here with no instance/group involved at all.
        CurrentServiceUserId = member.Id;
        await _sut.SetMyDayAsync(new SetDayAvailabilityRequest(monday, [new AvailabilityBlockInputDto(new TimeOnly(6, 0), new TimeOnly(7, 0))]));

        var result = await _sut.GetInstanceWeekAsync(instance.Id, monday);

        var memberDay = result.RoleGroups.Single().Members.Single(m => m.UserId == member.Id).Days.Single(d => d.Date == monday);
        memberDay.Blocks.ShouldHaveSingleItem();
        memberDay.Blocks[0].StartTime.ShouldBe(new TimeOnly(6, 0));
    }

    [Fact]
    public async Task GetInstanceWeekAsync_Throws_WhenCallerIsNotAnInstanceMember()
    {
        var (instance, _, _) = await CreateInstanceWithRoleAsync("Rowers");
        var outsider = await CreateUserAsync("outsider", "outsider@example.com");
        CurrentServiceUserId = outsider.Id;

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.GetInstanceWeekAsync(instance.Id, new DateOnly(2026, 7, 6)));
    }

    [Fact]
    public async Task GetInstanceWeekAsync_Throws_WhenGroupLacksOutingPlannerPermission()
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "NoPerm", Description = "Team", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var member = await CreateUserAsync("member-noperm", "member-noperm@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember { UserGroupInstanceId = instance.Id, UserId = member.Id });
        await Db.SaveChangesAsync();

        CurrentServiceUserId = member.Id;

        await Should.ThrowAsync<BlueValidationException>(() =>
            _sut.GetInstanceWeekAsync(instance.Id, new DateOnly(2026, 7, 6)));
    }

    [Fact]
    public async Task GetInstanceWeekAsync_IncludesOutings_ScheduledInThatWeek()
    {
        var (instance, member, _) = await CreateInstanceWithRoleAsync("Rowers");
        var monday = new DateOnly(2026, 7, 6);

        var inWeek = new Outing
        {
            Id = Guid.NewGuid(),
            UserGroupInstanceId = instance.Id,
            OutingDate = new DateTime(2026, 7, 8, 18, 0, 0, DateTimeKind.Utc),
            OutingDateEnd = new DateTime(2026, 7, 8, 20, 0, 0, DateTimeKind.Utc),
        };
        var outOfWeek = new Outing
        {
            Id = Guid.NewGuid(),
            UserGroupInstanceId = instance.Id,
            OutingDate = new DateTime(2026, 7, 20, 18, 0, 0, DateTimeKind.Utc),
        };
        Db.Outings.AddRange(inWeek, outOfWeek);
        await Db.SaveChangesAsync();

        CurrentServiceUserId = member.Id;
        var result = await _sut.GetInstanceWeekAsync(instance.Id, monday);

        // OutingDate/OutingDateEnd are stored as UTC; the service converts to local wall-clock
        // time before deriving Date/StartTime/EndTime, so compare against the same conversion
        // rather than a hardcoded offset that would only hold in one timezone.
        var expectedLocalStart = inWeek.OutingDate.ToLocalTime();
        var expectedLocalEnd = inWeek.OutingDateEnd!.Value.ToLocalTime();

        result.Outings.ShouldHaveSingleItem();
        result.Outings[0].Id.ShouldBe(inWeek.Id);
        result.Outings[0].Date.ShouldBe(DateOnly.FromDateTime(expectedLocalStart));
        result.Outings[0].StartTime.ShouldBe(TimeOnly.FromDateTime(expectedLocalStart));
        result.Outings[0].EndTime.ShouldBe(TimeOnly.FromDateTime(expectedLocalEnd));
    }

    // -------------------------------------------------------------------------
    // Test helpers
    // -------------------------------------------------------------------------

    private async Task<(UserGroupInstance instance, BlueUser member, UserGroupCategoryRole role)> CreateInstanceWithRoleAsync(string roleNamePlural)
    {
        var season = await CreateCurrentSeasonAsync();
        var category = new UserGroupCategory { Id = Guid.NewGuid(), Name = "General", Description = "General members" };
        var role = new UserGroupCategoryRole { Id = Guid.NewGuid(), UserGroupCategoryId = category.Id, NamePlural = roleNamePlural, SortOrder = 0 };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = $"Rowing {Guid.NewGuid():N}"[..12], Description = "Team", UserGroupCategoryId = category.Id };
        var instance = new UserGroupInstance { Id = Guid.NewGuid(), UserGroupId = group.Id, SeasonId = season.Id };
        var member = await CreateUserAsync($"member-{Guid.NewGuid():N}"[..16], $"{Guid.NewGuid():N}@example.com");

        Db.UserGroupCategories.Add(category);
        Db.UserGroupCategoryRoles.Add(role);
        Db.UserGroups.Add(group);
        Db.UserGroupInstances.Add(instance);
        Db.UserGroupInstanceMembers.Add(new UserGroupInstanceMember
        {
            UserGroupInstanceId = instance.Id,
            UserId = member.Id,
            UserGroupCategoryRoleId = role.Id,
        });
        Db.UserGroupPermissions.Add(new UserGroupPermission { UserGroupId = group.Id, Permission = BluePermission.OutingPlannerUse });
        await Db.SaveChangesAsync();

        return (instance, member, role);
    }
}
