using System.Text.Json;
using Bluewater.Core.Dto.Common;
using Bluewater.Core.Dto.Outings;
using Bluewater.Core.Dto.Profile;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Outings;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class OutingService : IOutingService
{
    private readonly BluewaterContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMaterialReservationService _materialReservationService;
    private readonly IValidator<UpsertOutingRequest> _outingValidator;
    private readonly IValidator<SetParticipantRoleRequest> _roleValidator;
    private readonly IValidator<InviteParticipantRequest> _inviteValidator;

    public OutingService(
        BluewaterContext db,
        ICurrentUserService currentUser,
        IMaterialReservationService materialReservationService,
        IValidator<UpsertOutingRequest> outingValidator,
        IValidator<SetParticipantRoleRequest> roleValidator,
        IValidator<InviteParticipantRequest> inviteValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _materialReservationService = materialReservationService;
        _outingValidator = outingValidator;
        _roleValidator = roleValidator;
        _inviteValidator = inviteValidator;
    }

    // -------------------------------------------------------------------------
    // Reads
    // -------------------------------------------------------------------------

    public async Task<List<OutingOverviewGroupDto>> GetOverviewAsync()
    {
        var currentSeasonId = await GetCurrentSeasonIdAsync();
        var userId = _currentUser.Id;

        var memberInstanceIds = await _db.UserGroupInstances
            .Where(x => x.SeasonId == currentSeasonId && x.Members.Any(m => m.UserId == userId))
            .Select(x => x.Id)
            .ToListAsync();

        var outings = await LoadOutingsQuery()
            .Where(o => !o.Confirmed
                && (memberInstanceIds.Contains(o.UserGroupInstanceId)
                    || o.Participants.Any(p => p.UserId == userId)))
            .OrderBy(o => o.OutingDate)
            .ToListAsync();

        return outings
            .GroupBy(o => o.UserGroupInstanceId)
            .Select(g => new OutingOverviewGroupDto(
                g.Key,
                InstanceName(g.First().UserGroupInstance),
                g.Select(o => ToListItemDto(o, userId)).ToList()))
            .ToList();
    }

    public async Task<List<OutingMyInstanceDto>> GetMyInstancesAsync()
    {
        var currentSeasonId = await GetCurrentSeasonIdAsync();
        var userId = _currentUser.Id;

        return await _db.UserGroupInstances
            .AsNoTracking()
            .Where(x => x.SeasonId == currentSeasonId
                && x.Members.Any(m => m.UserId == userId)
                && x.UserGroup.Permissions.Any(p => p.Permission == BluePermission.OutingPlannerUse))
            .Select(x => new OutingMyInstanceDto(x.Id, x.UserGroup.Name + " - " + x.Season.Name))
            .ToListAsync();
    }
    
    public async Task<List<OutingHistorySeasonGroupDto>> GetInstanceHistoryAsync()
    {
        var userId = _currentUser.Id;

        var instances = await _db.UserGroupInstances
            .AsNoTracking()
            .Where(x => x.Members.Any(m => m.UserId == userId)
                && x.UserGroup.Permissions.Any(p => p.Permission == BluePermission.OutingPlannerUse))
            .OrderBy(x => x.UserGroup.Name)
            .Select(x => new { x.SeasonId, x.Season.Name, x.Season.StartDate, Instance = new OutingMyInstanceDto(x.Id, x.UserGroup.Name) })
            .ToListAsync();

        return instances
            .GroupBy(x => (x.SeasonId, x.Name, x.StartDate))
            .OrderByDescending(g => g.Key.StartDate)
            .Select(g => new OutingHistorySeasonGroupDto(g.Key.SeasonId, g.Key.Name, g.Select(x => x.Instance).ToList()))
            .ToList();
    }

    public async Task<PagedResult<OutingListItemDto>> GetForInstanceAsync(Guid instanceId, OutingView view, int page, int pageSize)
    {
        await AssertInstanceMemberAsync(instanceId);

        var now = DateTime.UtcNow;
        var query = LoadOutingsQuery().Where(o => o.UserGroupInstanceId == instanceId);

        query = view switch
        {
            OutingView.Upcoming => query.Where(o => !o.Confirmed && o.OutingDate >= now).OrderBy(o => o.OutingDate),
            OutingView.AwaitingConfirmation => query.Where(o => !o.Confirmed && o.OutingDate < now).OrderByDescending(o => o.OutingDate),
            OutingView.RowedHistory => query.Where(o => o.Confirmed).OrderByDescending(o => o.OutingDate),
            _ => throw new ArgumentOutOfRangeException(nameof(view)),
        };

        var totalCount = await query.CountAsync();

        var items = view == OutingView.RowedHistory
            ? await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
            : await query.ToListAsync();

        var dtos = items.Select(o => ToListItemDto(o, _currentUser.Id)).ToList();
        return new PagedResult<OutingListItemDto>(dtos, page, pageSize, totalCount);
    }

    public async Task<OutingDetailDto> GetAsync(Guid outingId)
    {
        var outing = await LoadOutingWithDetailsAsync(outingId);

        if (!await CanAccessOutingAsync(outing))
            throw new BlueNotFoundException($"Outing '{outingId}' was not found.");

        var boatReservationId = await _db.MaterialReservations
            .AsNoTracking()
            .Where(r => r.OutingId == outingId)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync();

        return ToDetailDto(outing, boatReservationId);
    }

    public async Task<List<OutingChangelogEntryDto>> GetChangelogAsync(Guid outingId)
    {
        var outing = await FindOutingAsync(outingId);

        if (!await CanAccessOutingAsync(outing))
            throw new BlueNotFoundException($"Outing '{outingId}' was not found.");

        var entries = await _db.OutingChangelogEntries
            .AsNoTracking()
            .Where(e => e.OutingId == outingId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var actorIds = entries.Select(e => e.CreatedByUserId).Distinct().ToList();
        var actorNames = await _db.Users
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Fullname);

        return entries
            .Select(e => new OutingChangelogEntryDto(
                e.Id, e.Type, e.Fields, e.CreatedAt, e.CreatedByUserId,
                actorNames.GetValueOrDefault(e.CreatedByUserId, string.Empty)))
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Outing CRUD
    // -------------------------------------------------------------------------

    public async Task<OutingDetailDto> CreateAsync(UpsertOutingRequest request)
    {
        await _outingValidator.ValidateAndThrowAsync(request);
        await AssertInstanceMemberAsync(request.UserGroupInstanceId);
        await AssertInstanceGroupHasOutingPlannerPermissionAsync(request.UserGroupInstanceId);

        var outing = new Outing
        {
            Id = Guid.NewGuid(),
            UserGroupInstanceId = request.UserGroupInstanceId,
            OutingDate = request.OutingDate,
            OutingDateEnd = request.OutingDateEnd,
            BoatTypeId = request.BoatTypeId,
            BoatTypeDifferent = request.BoatTypeDifferent,
            BoatId = request.BoatId,
            Description = request.Description,
        };

        _db.Outings.Add(outing);
        await _db.SaveChangesAsync();

        return await GetAsync(outing.Id);
    }

    public async Task<OutingDetailDto> UpdateAsync(Guid outingId, UpsertOutingRequest request)
    {
        await _outingValidator.ValidateAndThrowAsync(request);

        var outing = await LoadOutingWithDetailsAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);
        AssertNotConfirmed(outing);

        var changelogEntries = new List<OutingChangelogEntry>();

        var boatChanged = outing.BoatId != request.BoatId;
        var dateChanged = outing.OutingDate != request.OutingDate || outing.OutingDateEnd != request.OutingDateEnd;
        if (boatChanged || dateChanged)
        {
            var hadLinkedReservation = await _db.MaterialReservations.AnyAsync(r => r.OutingId == outingId);
            if (hadLinkedReservation)
            {
                await _materialReservationService.DeleteLinkedForOutingAsync(outingId);
                changelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.BoatReservationRemoved, new { }));
            }
        }

        if (outing.OutingDate != request.OutingDate)
        {
            changelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.DateChanged,
                new { from = outing.OutingDate, to = request.OutingDate }));

            foreach (var participant in outing.Participants)
                participant.CheckedIn = false;
        }

        if (outing.BoatId != request.BoatId)
        {
            var newBoat = request.BoatId.HasValue
                ? await _db.Equipment.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BoatId)
                : null;

            changelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.BoatChanged, new
            {
                fromBoatId = outing.BoatId,
                fromBoatName = outing.Boat?.Name,
                toBoatId = request.BoatId,
                toBoatName = newBoat?.Name,
            }));
        }

        if (outing.BoatTypeId != request.BoatTypeId)
        {
            var newBoatType = request.BoatTypeId.HasValue
                ? await _db.EquipmentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BoatTypeId)
                : null;

            changelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.BoatTypeChanged, new
            {
                fromBoatTypeId = outing.BoatTypeId,
                fromBoatTypeName = outing.BoatType?.Name,
                toBoatTypeId = request.BoatTypeId,
                toBoatTypeName = newBoatType?.Name,
            }));

            ApplyCapacityRulesOnBoatTypeChange(outing, newBoatType, changelogEntries);
        }

        outing.OutingDate = request.OutingDate;
        outing.OutingDateEnd = request.OutingDateEnd;
        outing.BoatTypeId = request.BoatTypeId;
        outing.BoatTypeDifferent = request.BoatTypeDifferent;
        outing.BoatId = request.BoatId;
        outing.Description = request.Description;

        _db.OutingChangelogEntries.AddRange(changelogEntries);
        await _db.SaveChangesAsync();

        return await GetAsync(outingId);
    }

    public async Task DeleteAsync(Guid outingId)
    {
        var outing = await FindOutingAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);
        AssertNotConfirmed(outing);

        await _materialReservationService.DeleteLinkedForOutingAsync(outingId);

        _db.Outings.Remove(outing);
        await _db.SaveChangesAsync();
    }

    public async Task<OutingDetailDto> BookBoatAsync(Guid outingId)
    {
        var outing = await LoadOutingWithDetailsAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);
        AssertNotConfirmed(outing);

        if (outing.BoatId is null)
            throw new BlueValidationException("This outing does not have a boat selected.");

        if (outing.OutingDateEnd is null)
            throw new BlueValidationException("This outing needs an end time before its boat can be reserved.");

        var alreadyLinked = await _db.MaterialReservations.AnyAsync(r => r.OutingId == outingId);
        if (alreadyLinked)
            throw new BlueValidationException("This outing's boat has already been reserved.");

        // OutingDate/OutingDateEnd are stored as UTC instants, but MaterialReservation's
        // Date/StartTime/EndTime are plain local wall-clock values (no timezone, per the
        // Material Planner's own convention) — convert before deriving them, otherwise the
        // linked reservation silently lands on the wrong time slot relative to the outing.
        var localStart = outing.OutingDate.ToLocalTime();
        var localEnd = outing.OutingDateEnd.Value.ToLocalTime();

        var label = $"{InstanceName(outing.UserGroupInstance)} - {outing.OutingDate:d MMM}";
        await _materialReservationService.CreateLinkedForOutingAsync(
            outingId,
            outing.BoatId.Value,
            DateOnly.FromDateTime(localStart),
            TimeOnly.FromDateTime(localStart),
            TimeOnly.FromDateTime(localEnd),
            label);

        _db.OutingChangelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.BoatReserved, new
        {
            boatId = outing.BoatId,
            boatName = outing.Boat?.Name,
        }));
        await _db.SaveChangesAsync();

        return await GetAsync(outingId);
    }

    // -------------------------------------------------------------------------
    // Participants
    // -------------------------------------------------------------------------

    public async Task<List<ActiveMemberDto>> SearchCandidatesAsync(Guid outingId, string? search, CancellationToken ct = default)
    {
        var outing = await FindOutingAsync(outingId);

        if (!await CanAccessOutingAsync(outing))
            throw new BlueNotFoundException($"Outing '{outingId}' was not found.");

        var query = _db.Users
            .AsNoTracking()
            .Where(u =>
                _db.UserGroupInstanceMembers.Any(m => m.UserGroupInstanceId == outing.UserGroupInstanceId
                    && m.UserId == u.Id
                    && m.DeletedAt == null)
                || _db.OutingParticipants.Any(p => p.OutingId == outingId && p.Invited && p.UserId == u.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Firstname.ToLower().Contains(term) ||
                u.Surname.ToLower().Contains(term) ||
                u.SurnamePrefix.ToLower().Contains(term));
        }

        var users = await query
            .OrderBy(u => u.Surname).ThenBy(u => u.Firstname)
            .ToListAsync(ct);

        return users
            .Select(u => new ActiveMemberDto(u.Id, u.Fullname, u.ProfilePictureFileId != null))
            .ToList();
    }

    public async Task<OutingDetailDto> SetParticipantRoleAsync(Guid outingId, Guid userId, SetParticipantRoleRequest request)
    {
        await _roleValidator.ValidateAndThrowAsync(request);

        var outing = await LoadOutingWithDetailsAsync(outingId);

        if (!await CanAccessOutingAsync(outing))
            throw new BlueNotFoundException($"Outing '{outingId}' was not found.");

        AssertNotConfirmed(outing);

        var isInstanceMember = await IsInstanceMemberAsync(outing.UserGroupInstanceId, _currentUser.Id);
        if (!isInstanceMember && userId != _currentUser.Id)
            throw new BlueValidationException("You can only change your own role on this outing.");

        var participant = outing.Participants.FirstOrDefault(p => p.UserId == userId);
        var isFirstSignUp = participant == null || participant.Role == OutingParticipantRole.None;

        if (participant == null)
        {
            participant = new OutingParticipant
            {
                OutingId = outingId,
                UserId = userId,
                Role = OutingParticipantRole.None,
                Invited = false,
            };
            _db.OutingParticipants.Add(participant);
        }

        var targetUser = participant.User ?? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var fromRole = participant.Role;
        var requestedRole = request.Role;
        var appliedRole = requestedRole;
        string? reason = null;

        if (requestedRole == OutingParticipantRole.Rower && outing.BoatType != null)
        {
            var otherRowerCount = outing.Participants.Count(p => p.Role == OutingParticipantRole.Rower && p.UserId != userId);
            if (otherRowerCount >= outing.BoatType.RowersCount)
            {
                appliedRole = OutingParticipantRole.Reserve;
                reason = "capacity";
            }
        }
        else if (requestedRole == OutingParticipantRole.Cox && outing.BoatType != null && !outing.BoatType.Coxed)
        {
            appliedRole = OutingParticipantRole.Reserve;
            reason = "not_coxed";
        }

        participant.Role = appliedRole;

        var changelogType = isFirstSignUp ? OutingChangelogType.FirstSignUp : OutingChangelogType.RoleChanged;
        _db.OutingChangelogEntries.Add(BuildChangelogEntry(outingId, changelogType, new
        {
            userId,
            userFullname = targetUser?.Fullname,
            from = fromRole.ToString(),
            requestedRole = requestedRole.ToString(),
            appliedRole = appliedRole.ToString(),
            reason,
        }));

        await _db.SaveChangesAsync();
        return await GetAsync(outingId);
    }

    public async Task<OutingDetailDto> InviteParticipantAsync(Guid outingId, InviteParticipantRequest request)
    {
        await _inviteValidator.ValidateAndThrowAsync(request);

        var outing = await LoadOutingWithDetailsAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);
        AssertNotConfirmed(outing);

        var existing = outing.Participants.FirstOrDefault(p => p.UserId == request.UserId);
        if (existing == null)
        {
            _db.OutingParticipants.Add(new OutingParticipant
            {
                OutingId = outingId,
                UserId = request.UserId,
                Role = OutingParticipantRole.None,
                Invited = true,
            });
        }
        else
        {
            existing.Invited = true;
        }

        var invitedUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId);
        _db.OutingChangelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.Invited, new
        {
            userId = request.UserId,
            userFullname = invitedUser?.Fullname,
        }));

        await _db.SaveChangesAsync();
        return await GetAsync(outingId);
    }

    public async Task<OutingDetailDto> RemoveParticipantAsync(Guid outingId, Guid userId)
    {
        var outing = await LoadOutingWithDetailsAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);
        AssertNotConfirmed(outing);

        var participant = outing.Participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new BlueNotFoundException($"Participant '{userId}' was not found on outing '{outingId}'.");

        _db.OutingParticipants.Remove(participant);
        await _db.SaveChangesAsync();

        // The soft-deleted entity gets detached above (BluewaterContext.DetachSoftDeleted), but a
        // detach doesn't retroactively evict it from a navigation collection already loaded into
        // memory by an earlier Include — evict it explicitly so the re-fetch below doesn't return it.
        outing.Participants.Remove(participant);

        return await GetAsync(outingId);
    }

    public async Task<OutingDetailDto> CheckInAsync(Guid outingId)
    {
        var outing = await LoadOutingWithDetailsAsync(outingId);

        if (!await CanAccessOutingAsync(outing))
            throw new BlueNotFoundException($"Outing '{outingId}' was not found.");

        var now = DateTime.UtcNow;
        if (now < outing.OutingDate.AddMinutes(-30) || now > outing.OutingDate.AddHours(3))
            throw new BlueValidationException("Check-in is only allowed from 30 minutes before to 3 hours after the outing.");

        var participant = outing.Participants.FirstOrDefault(p => p.UserId == _currentUser.Id)
            ?? throw new BlueValidationException("You are not a participant on this outing.");

        participant.CheckedIn = true;
        await _db.SaveChangesAsync();

        return await GetAsync(outingId);
    }

    // -------------------------------------------------------------------------
    // Confirmation
    // -------------------------------------------------------------------------

    public async Task<OutingDetailDto> ConfirmAsync(Guid outingId)
    {
        var outing = await FindOutingAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);

        if (outing.Confirmed)
            throw new BlueValidationException("This outing has already been confirmed.");

        if (outing.OutingDate > DateTime.UtcNow)
            throw new BlueValidationException("This outing cannot be confirmed before it has taken place.");

        outing.Confirmed = true;
        _db.OutingChangelogEntries.Add(BuildChangelogEntry(outingId, OutingChangelogType.Confirmed, new { }));

        await _db.SaveChangesAsync();
        return await GetAsync(outingId);
    }

    public async Task MarkDidNotHappenAsync(Guid outingId)
    {
        var outing = await FindOutingAsync(outingId);
        await AssertInstanceMemberAsync(outing.UserGroupInstanceId);

        if (outing.Confirmed)
            throw new BlueValidationException("This outing has already been confirmed.");

        if (outing.OutingDate > DateTime.UtcNow)
            throw new BlueValidationException("This outing cannot be marked as not happened before it has taken place.");

        _db.Outings.Remove(outing);
        await _db.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void ApplyCapacityRulesOnBoatTypeChange(Outing outing, EquipmentType? newBoatType, List<OutingChangelogEntry> changelogEntries)
    {
        if (newBoatType == null)
            return;

        var rowers = outing.Participants
            .Where(p => p.Role == OutingParticipantRole.Rower)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToList();

        var excess = rowers.Count - newBoatType.RowersCount;
        for (var i = 0; i < excess; i++)
        {
            var demoted = rowers[i];
            demoted.Role = OutingParticipantRole.Reserve;
            changelogEntries.Add(BuildChangelogEntry(outing.Id, OutingChangelogType.RoleChanged, new
            {
                userId = demoted.UserId,
                userFullname = demoted.User?.Fullname,
                from = "Rower",
                requestedRole = "Rower",
                appliedRole = "Reserve",
                reason = "capacity",
            }));
        }

        if (!newBoatType.Coxed)
        {
            var cox = outing.Participants.FirstOrDefault(p => p.Role == OutingParticipantRole.Cox);
            if (cox != null)
            {
                cox.Role = OutingParticipantRole.Reserve;
                changelogEntries.Add(BuildChangelogEntry(outing.Id, OutingChangelogType.RoleChanged, new
                {
                    userId = cox.UserId,
                    userFullname = cox.User?.Fullname,
                    from = "Cox",
                    requestedRole = "Cox",
                    appliedRole = "Reserve",
                    reason = "not_coxed",
                }));
            }
        }
    }

    private static OutingChangelogEntry BuildChangelogEntry(Guid outingId, OutingChangelogType type, object fields) => new()
    {
        Id = Guid.NewGuid(),
        OutingId = outingId,
        Type = type,
        Fields = JsonSerializer.Serialize(fields),
    };

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
            throw new BlueValidationException("This team is not allowed to use the outing planner.");
    }

    private async Task<bool> CanAccessOutingAsync(Outing outing)
    {
        if (await IsInstanceMemberAsync(outing.UserGroupInstanceId, _currentUser.Id))
            return true;

        return outing.Participants.Any(p => p.UserId == _currentUser.Id)
            || await _db.OutingParticipants.AnyAsync(p => p.OutingId == outing.Id && p.UserId == _currentUser.Id);
    }

    private static void AssertNotConfirmed(Outing outing)
    {
        if (outing.Confirmed)
            throw new BlueValidationException("This outing has been confirmed and can no longer be changed.");
    }

    private async Task<Guid> GetCurrentSeasonIdAsync()
    {
        var settings = await _db.AppSettings.FirstAsync();
        return settings.CurrentSeasonId;
    }

    private IQueryable<Outing> LoadOutingsQuery()
    {
        return _db.Outings
            .AsNoTracking()
            .Include(o => o.UserGroupInstance).ThenInclude(i => i.UserGroup)
            .Include(o => o.UserGroupInstance).ThenInclude(i => i.Season)
            .Include(o => o.BoatType)
            .Include(o => o.Boat)
            .Include(o => o.Participants).ThenInclude(p => p.User);
    }

    private async Task<Outing> LoadOutingWithDetailsAsync(Guid outingId)
    {
        return await _db.Outings
            .Include(o => o.UserGroupInstance).ThenInclude(i => i.UserGroup)
            .Include(o => o.UserGroupInstance).ThenInclude(i => i.Season)
            .Include(o => o.BoatType)
            .Include(o => o.Boat)
            .Include(o => o.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(o => o.Id == outingId)
            ?? throw new BlueNotFoundException($"Outing '{outingId}' was not found.");
    }

    private async Task<Outing> FindOutingAsync(Guid outingId)
    {
        return await _db.Outings
            .Include(o => o.Participants)
            .FirstOrDefaultAsync(o => o.Id == outingId)
            ?? throw new BlueNotFoundException($"Outing '{outingId}' was not found.");
    }

    private static string InstanceName(Domain.Models.Groups.UserGroupInstance instance) =>
        $"{instance.UserGroup.Name} - {instance.Season.Name}";

    private static OutingListItemDto ToListItemDto(Outing outing, Guid currentUserId)
    {
        var myParticipant = outing.Participants.FirstOrDefault(p => p.UserId == currentUserId);

        return new OutingListItemDto(
            outing.Id,
            outing.UserGroupInstanceId,
            InstanceName(outing.UserGroupInstance),
            outing.OutingDate,
            outing.OutingDateEnd,
            outing.BoatTypeId,
            outing.BoatType?.Name,
            outing.BoatTypeDifferent,
            outing.BoatId,
            outing.Boat?.Name,
            outing.Confirmed,
            outing.Participants.Count(p => p.Role == OutingParticipantRole.Rower),
            outing.BoatType?.RowersCount,
            outing.Participants.Any(p => p.Role == OutingParticipantRole.Cox),
            outing.BoatType?.Coxed ?? false,
            myParticipant?.Role,
            myParticipant?.CheckedIn ?? false,
            outing.Participants
                .Where(p => p.Role != OutingParticipantRole.None)
                .Select(p => new OutingParticipantDto(
                    p.UserId,
                    p.User?.Fullname ?? string.Empty,
                    p.User?.ProfilePictureFileId != null,
                    p.Role,
                    p.Invited,
                    p.CheckedIn,
                    p.UpdatedAt))
                .ToList());
    }

    private static OutingDetailDto ToDetailDto(Outing outing, Guid? boatReservationId)
    {
        return new OutingDetailDto(
            outing.Id,
            outing.UserGroupInstanceId,
            InstanceName(outing.UserGroupInstance),
            outing.OutingDate,
            outing.OutingDateEnd,
            outing.BoatTypeId,
            outing.BoatType?.Name,
            outing.BoatTypeDifferent,
            outing.BoatType?.RowersCount,
            outing.BoatType?.Coxed ?? false,
            outing.BoatId,
            outing.Boat?.Name,
            boatReservationId,
            outing.Description,
            outing.Confirmed,
            outing.Participants
                .Select(p => new OutingParticipantDto(
                    p.UserId,
                    p.User?.Fullname ?? string.Empty,
                    p.User?.ProfilePictureFileId != null,
                    p.Role,
                    p.Invited,
                    p.CheckedIn,
                    p.UpdatedAt))
                .ToList());
    }
}
