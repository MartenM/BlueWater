using Bluewater.Core.Dto.Availability;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Availability;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Outings;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly BluewaterContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<SetDayAvailabilityRequest> _setDayValidator;

    public AvailabilityService(
        BluewaterContext db,
        ICurrentUserService currentUser,
        IValidator<SetDayAvailabilityRequest> setDayValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _setDayValidator = setDayValidator;
    }

    public async Task<MyWeekAvailabilityDto> GetMyWeekAsync(DateOnly weekStart)
    {
        var monday = ToMonday(weekStart);
        var days = await LoadDaysForUsersAsync([_currentUser.Id], monday);
        return new MyWeekAvailabilityDto(monday, days[_currentUser.Id]);
    }

    public async Task<List<AvailabilityBlockDto>> SetMyDayAsync(SetDayAvailabilityRequest request)
    {
        await _setDayValidator.ValidateAndThrowAsync(request);

        var userId = _currentUser.Id;

        var existing = await _db.AvailabilityBlocks
            .Where(b => b.UserId == userId && b.Date == request.Date)
            .ToListAsync();
        _db.AvailabilityBlocks.RemoveRange(existing);

        var blocks = request.Blocks
            .Select(b => new AvailabilityBlock
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = request.Date,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
            })
            .ToList();
        _db.AvailabilityBlocks.AddRange(blocks);

        await _db.SaveChangesAsync();

        return blocks
            .OrderBy(b => b.StartTime)
            .Select(ToDto)
            .ToList();
    }

    public async Task<InstanceWeekAvailabilityDto> GetInstanceWeekAsync(Guid instanceId, DateOnly weekStart)
    {
        await AssertInstanceMemberAsync(instanceId);
        await AssertInstanceGroupHasOutingPlannerPermissionAsync(instanceId);

        var monday = ToMonday(weekStart);

        var members = await _db.UserGroupInstanceMembers
            .AsNoTracking()
            .Where(m => m.UserGroupInstanceId == instanceId)
            .Include(m => m.User)
            .Include(m => m.UserGroupCategoryRole)
            .ToListAsync();

        var userIds = members.Select(m => m.UserId).ToList();
        var days = await LoadDaysForUsersAsync(userIds, monday);
        var outings = await LoadOutingsForWeekAsync(instanceId, monday);

        var roleGroups = members
            .GroupBy(m => (m.UserGroupCategoryRoleId, RoleLabel: m.UserGroupCategoryRole?.NamePlural ?? "Overig"))
            .OrderBy(g => g.Key.RoleLabel)
            .Select(g => new AvailabilityRoleGroupDto(
                g.Key.UserGroupCategoryRoleId,
                g.Key.RoleLabel,
                g.OrderBy(m => m.User.Surname).ThenBy(m => m.User.Firstname)
                    .Select(m => new AvailabilityMemberWeekDto(
                        m.UserId,
                        m.User.Fullname,
                        m.User.ProfilePictureFileId != null,
                        days[m.UserId]))
                    .ToList()))
            .ToList();

        return new InstanceWeekAvailabilityDto(instanceId, monday, roleGroups, outings);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<List<OutingTimelineEntryDto>> LoadOutingsForWeekAsync(Guid instanceId, DateOnly monday)
    {
        // OutingDate is stored as a UTC instant, but this overlay renders on the same local
        // wall-clock time axis as AvailabilityBlock — convert the week bounds to UTC for the
        // query, then convert each match back to local before deriving Date/StartTime/EndTime,
        // otherwise outings show up shifted by the server's UTC offset (or on the wrong day
        // near midnight).
        var weekStart = DateTime.SpecifyKind(monday.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local)
            .ToUniversalTime();
        var weekEnd = DateTime.SpecifyKind(monday.AddDays(7).ToDateTime(TimeOnly.MinValue), DateTimeKind.Local)
            .ToUniversalTime();

        var outings = await _db.Outings
            .AsNoTracking()
            .Include(o => o.Boat)
            .Where(o => o.UserGroupInstanceId == instanceId && o.OutingDate >= weekStart && o.OutingDate < weekEnd)
            .OrderBy(o => o.OutingDate)
            .ToListAsync();

        return outings
            .Select(o =>
            {
                var localStart = o.OutingDate.ToLocalTime();
                var localEnd = o.OutingDateEnd?.ToLocalTime();
                return new OutingTimelineEntryDto(
                    o.Id,
                    DateOnly.FromDateTime(localStart),
                    TimeOnly.FromDateTime(localStart),
                    localEnd.HasValue ? TimeOnly.FromDateTime(localEnd.Value) : null,
                    o.Boat?.Name,
                    o.Boat?.Name ?? o.BoatTypeDifferent ?? "Outing");
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, List<AvailabilityDayDto>>> LoadDaysForUsersAsync(List<Guid> userIds, DateOnly monday)
    {
        var weekEnd = monday.AddDays(6);

        var blocks = await _db.AvailabilityBlocks
            .AsNoTracking()
            .Where(b => userIds.Contains(b.UserId) && b.Date >= monday && b.Date <= weekEnd)
            .ToListAsync();

        var blocksByUserAndDate = blocks
            .GroupBy(b => (b.UserId, b.Date))
            .ToDictionary(g => g.Key, g => g.OrderBy(b => b.StartTime).Select(ToDto).ToList());

        var result = new Dictionary<Guid, List<AvailabilityDayDto>>();
        foreach (var userId in userIds)
        {
            var userDays = new List<AvailabilityDayDto>();
            for (var i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                var dayBlocks = blocksByUserAndDate.GetValueOrDefault((userId, date), []);
                userDays.Add(new AvailabilityDayDto(date, dayBlocks));
            }

            result[userId] = userDays;
        }

        return result;
    }

    private static DateOnly ToMonday(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static AvailabilityBlockDto ToDto(AvailabilityBlock block) =>
        new(block.Id, block.Date, block.StartTime, block.EndTime);

    private async Task AssertInstanceMemberAsync(Guid instanceId)
    {
        if (!await IsInstanceMemberAsync(instanceId, _currentUser.Id))
            throw new BlueValidationException("You are not a member of this team.");
    }

    private async Task<bool> IsInstanceMemberAsync(Guid instanceId, Guid userId)
    {
        return await _db.UserGroupInstanceMembers
            .AnyAsync(m => m.UserGroupInstanceId == instanceId && m.UserId == userId);
    }

    private async Task AssertInstanceGroupHasOutingPlannerPermissionAsync(Guid instanceId)
    {
        var groupId = await _db.UserGroupInstances
            .Where(i => i.Id == instanceId)
            .Select(i => i.UserGroupId)
            .FirstAsync();

        var hasPermission = await _db.UserGroupPermissions
            .AnyAsync(p => p.UserGroupId == groupId && p.Permission == BluePermission.OutingPlannerUse);

        if (!hasPermission)
            throw new BlueValidationException("This team is not allowed to use the availability planner.");
    }
}
