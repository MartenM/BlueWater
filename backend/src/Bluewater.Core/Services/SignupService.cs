using System.Text;
using Bluewater.Core.Dto.Signup;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Options;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Domain.Models.Clusters;
using Bluewater.Domain.Models.Groups;
using Bluewater.Domain.Models.Signup;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bluewater.Core.Services;

public class SignupService : ISignupService
{
    private readonly BluewaterContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly SignupOptions _options;
    private readonly IValidator<UpsertSignupRequest> _signupValidator;
    private readonly IValidator<SubmitResponseRequest> _responseValidator;

    public SignupService(
        BluewaterContext db,
        ICurrentUserService currentUser,
        IOptions<SignupOptions> options,
        IValidator<UpsertSignupRequest> signupValidator,
        IValidator<SubmitResponseRequest> responseValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _options = options.Value;
        _signupValidator = signupValidator;
        _responseValidator = responseValidator;
    }

    // -------------------------------------------------------------------------
    // Member
    // -------------------------------------------------------------------------

    public async Task<List<SignupListItemDto>> ListForMemberAsync()
    {
        var accessibleClusterIds = await GetAccessibleClusterIdsAsync(_currentUser.Id);
        var cutoff = DateTime.UtcNow.AddDays(-_options.HideAfterDays);

        var signups = await _db.Signups
            .Include(s => s.Category)
            .Include(s => s.Clusters)
            .Include(s => s.Responses)
            .Where(s => s.Clusters.Any(sc => accessibleClusterIds.Contains(sc.MemberClusterId)))
            .Where(s => s.EndDate == null || s.EndDate.Value > cutoff)
            .OrderBy(s => s.Category != null ? s.Category.SortOrder : int.MaxValue)
            .ThenBy(s => s.EndDate)
            .ThenBy(s => s.Title)
            .ToListAsync();

        return signups.Select(s => ToListItemDto(s, _currentUser.Id)).ToList();
    }

    public async Task<SignupDetailDto> GetForMemberAsync(Guid signupId)
    {
        var accessibleClusterIds = await GetAccessibleClusterIdsAsync(_currentUser.Id);

        var signup = await LoadSignupWithDetails(signupId);

        if (!signup.Clusters.Any(sc => accessibleClusterIds.Contains(sc.MemberClusterId)))
            throw new BlueNotFoundException($"Signup '{signupId}' was not found.");

        var orderedResponses = OrderResponses(signup.Responses.ToList());

        List<SignupResponsePublicDto>? publicResponses = null;
        if (!signup.HideSignups)
        {
            publicResponses = orderedResponses.Select(r =>
                new SignupResponsePublicDto(
                    r.Id,
                    signup.Anonymous ? null : r.UserId,
                    signup.Anonymous ? null : r.User?.Fullname,
                    r.Reservation,
                    ComputeStatus(r, orderedResponses, signup.MaxSignups),
                    r.CreatedAt,
                    r.FieldValues
                        .Where(v => v.Field.Visible)
                        .OrderBy(v => v.Field.SortOrder)
                        .Select(v => new SignupResponseFieldValueDto(v.FieldId, v.Field.Title, v.Value))
                        .ToList()
                )).ToList();
        }

        var myResponse = orderedResponses.FirstOrDefault(r => r.UserId == _currentUser.Id);

        var visibleFields = signup.InputFields
            .Where(f => f.Visible)
            .OrderBy(f => f.SortOrder)
            .Select(ToFieldDto)
            .ToList();

        return new SignupDetailDto(
            signup.Id, signup.Title, signup.Description,
            signup.CategoryId, signup.Category?.Title,
            signup.EndDate, signup.AllowMultiple, signup.AllowDelete, signup.AllowUpdate,
            signup.MaxSignups, signup.MaxWaitlist, signup.HideSignups, signup.Anonymous,
            visibleFields,
            publicResponses,
            myResponse != null ? ToResponseDto(myResponse, orderedResponses, signup.MaxSignups, signupId) : null
        );
    }

    public async Task<List<SignupListItemDto>> GetMyResponsesAsync()
    {
        var signupIds = await _db.SignupResponses
            .Where(r => r.UserId == _currentUser.Id)
            .Select(r => r.SignupId)
            .Distinct()
            .ToListAsync();

        var signups = await _db.Signups
            .Include(s => s.Category)
            .Include(s => s.Responses)
            .Where(s => signupIds.Contains(s.Id))
            .OrderByDescending(s => s.EndDate)
            .ToListAsync();

        return signups.Select(s => ToListItemDto(s, _currentUser.Id)).ToList();
    }

    public async Task<SignupResponseDto> SubmitResponseAsync(Guid signupId, SubmitResponseRequest request)
    {
        await _responseValidator.ValidateAndThrowAsync(request);

        var accessibleClusterIds = await GetAccessibleClusterIdsAsync(_currentUser.Id);
        var signup = await LoadSignupWithDetails(signupId);

        if (!signup.Clusters.Any(sc => accessibleClusterIds.Contains(sc.MemberClusterId)))
            throw new BlueNotFoundException($"Signup '{signupId}' was not found.");

        if (signup.EndDate.HasValue && signup.EndDate.Value < DateTime.UtcNow)
            throw new BlueValidationException("This signup has closed.");

        if (!signup.AllowMultiple)
        {
            var alreadyResponded = signup.Responses.Any(r => r.UserId == _currentUser.Id);
            if (alreadyResponded)
                throw new BlueValidationException("You have already signed up and multiple responses are not allowed.");
        }

        // Check if both the signup list and waitlist are full (only possible when both limits are set)
        if (signup.MaxSignups.HasValue && signup.MaxWaitlist.HasValue)
        {
            var activeResponses = signup.Responses.Count;
            if (activeResponses >= signup.MaxSignups.Value + signup.MaxWaitlist.Value)
                throw new BlueValidationException("This signup is full, including the waitlist.");
        }

        var response = new SignupResponse
        {
            Id = Guid.NewGuid(),
            SignupId = signupId,
            UserId = _currentUser.Id,
            Reservation = false,
            FieldValues = MapFieldValues(request.FieldValues, signup.InputFields),
        };

        _db.SignupResponses.Add(response);
        await _db.SaveChangesAsync();

        await _db.Entry(response).Reference(r => r.User).LoadAsync();

        var allResponses = OrderResponses(signup.Responses.Append(response).ToList());
        return ToResponseDto(response, allResponses, signup.MaxSignups, signupId);
    }

    public async Task<SignupResponseDto> UpdateMyResponseAsync(Guid signupId, Guid responseId, UpdateResponseRequest request)
    {
        var signup = await LoadSignupWithDetails(signupId);
        var response = FindMyResponse(signup, responseId);

        if (!signup.AllowUpdate)
            throw new BlueValidationException("Updates to this signup are not allowed.");

        // Remove old values first; do NOT replace the navigation property afterwards —
        // doing so would trigger EF's orphan detection and generate a second DELETE for the
        // same rows, causing a DbUpdateConcurrencyException (0 rows affected on the second pass).
        _db.SignupResponseFieldValues.RemoveRange(response.FieldValues.ToList());

        var newValues = MapFieldValues(request.FieldValues, signup.InputFields);
        foreach (var v in newValues)
            v.ResponseId = response.Id;
        _db.SignupResponseFieldValues.AddRange(newValues);

        await _db.SaveChangesAsync();

        // Reload field values with Field navigation so ToResponseDto can read Field.Title/SortOrder.
        await _db.Entry(response).Collection(r => r.FieldValues).Query()
            .Include(v => v.Field)
            .LoadAsync();

        var allResponses = OrderResponses(signup.Responses.ToList());
        return ToResponseDto(response, allResponses, signup.MaxSignups, signupId);
    }

    public async Task DeleteMyResponseAsync(Guid signupId, Guid responseId)
    {
        var signup = await LoadSignupWithDetails(signupId);
        var response = FindMyResponse(signup, responseId);

        if (!signup.AllowDelete)
            throw new BlueValidationException("Deleting responses from this signup is not allowed.");

        _db.SignupResponses.Remove(response);
        await _db.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<List<SignupListItemDto>> AdminListAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_options.HideAfterDays);

        var query = _db.Signups
            .Include(s => s.Category)
            .Include(s => s.Responses)
            .Where(s => s.EndDate == null || s.EndDate.Value > cutoff)
            .AsQueryable();

        if (!_currentUser.HasPermission(BluePermission.AdminSignupModifyOthers))
            query = query.Where(s => s.CreatedByUserId == _currentUser.Id);

        var signups = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return signups.Select(s => ToListItemDto(s, null)).ToList();
    }

    public async Task<List<SignupArchiveSeasonDto>> AdminListArchivedAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_options.HideAfterDays);

        var query = _db.Signups
            .Include(s => s.Category)
            .Include(s => s.Responses)
            .Where(s => s.EndDate != null && s.EndDate.Value <= cutoff)
            .AsQueryable();

        if (!_currentUser.HasPermission(BluePermission.AdminSignupModifyOthers))
            query = query.Where(s => s.CreatedByUserId == _currentUser.Id);

        var signups = await query
            .OrderByDescending(s => s.EndDate)
            .ToListAsync();

        var seasons = await _db.Seasons
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        var groups = signups
            .GroupBy(s =>
            {
                var createdDate = DateOnly.FromDateTime(s.CreatedAt);
                var season = seasons.FirstOrDefault(se => createdDate >= se.StartDate && createdDate <= se.EndDate);
                return season?.Name ?? s.CreatedAt.Year.ToString();
            })
            .Select(g => new SignupArchiveSeasonDto(g.Key, g.Select(s => ToListItemDto(s, null)).ToList()))
            .ToList();

        return groups;
    }

    public async Task<SignupAdminDetailDto> AdminGetAsync(Guid signupId)
    {
        var signup = await LoadSignupWithDetails(signupId);
        return ToAdminDetailDto(signup);
    }

    public async Task<SignupAdminDetailDto> AdminCreateAsync(UpsertSignupRequest request)
    {
        await _signupValidator.ValidateAndThrowAsync(request);

        var signup = new Domain.Models.Signup.Signup
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            EndDate = request.EndDate,
            AllowMultiple = request.AllowMultiple,
            AllowDelete = request.AllowDelete,
            AllowUpdate = request.AllowUpdate,
            MaxSignups = request.MaxSignups,
            MaxWaitlist = request.MaxWaitlist,
            HideSignups = request.HideSignups,
            Anonymous = request.Anonymous,
            Clusters = request.ClusterIds
                .Select(id => new SignupCluster { MemberClusterId = id })
                .ToList(),
        };

        _db.Signups.Add(signup);
        await _db.SaveChangesAsync();

        return await AdminGetAsync(signup.Id);
    }

    public async Task<SignupAdminDetailDto> AdminUpdateAsync(Guid id, UpsertSignupRequest request)
    {
        await _signupValidator.ValidateAndThrowAsync(request);

        var signup = await LoadSignupWithDetails(id);
        AssertAdminAccess(signup);

        signup.Title = request.Title;
        signup.Description = request.Description;
        signup.CategoryId = request.CategoryId;
        signup.EndDate = request.EndDate;
        signup.AllowMultiple = request.AllowMultiple;
        signup.AllowDelete = request.AllowDelete;
        signup.AllowUpdate = request.AllowUpdate;
        signup.MaxSignups = request.MaxSignups;
        signup.MaxWaitlist = request.MaxWaitlist;
        signup.HideSignups = request.HideSignups;
        signup.Anonymous = request.Anonymous;

        // Replace cluster links
        _db.SignupClusters.RemoveRange(signup.Clusters);
        signup.Clusters = request.ClusterIds
            .Select(clusterId => new SignupCluster { SignupId = signup.Id, MemberClusterId = clusterId })
            .ToList();

        await _db.SaveChangesAsync();

        return await AdminGetAsync(signup.Id);
    }

    public async Task AdminDeleteAsync(Guid id)
    {
        var signup = await _db.Signups.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new BlueNotFoundException($"Signup '{id}' was not found.");

        AssertAdminAccess(signup);

        _db.Signups.Remove(signup);
        await _db.SaveChangesAsync();
    }

    public async Task<SignupInputFieldDto> AdminAddFieldAsync(Guid signupId, UpsertSignupInputFieldRequest request)
    {
        var signup = await _db.Signups.FirstOrDefaultAsync(s => s.Id == signupId)
            ?? throw new BlueNotFoundException($"Signup '{signupId}' was not found.");
        AssertAdminAccess(signup);

        var field = new SignupInputField
        {
            Id = Guid.NewGuid(),
            SignupId = signupId,
            Title = request.Title,
            Note = request.Note,
            Type = request.Type,
            Options = request.Options,
            Visible = request.Visible,
            SortOrder = request.SortOrder,
        };

        _db.SignupInputFields.Add(field);
        await _db.SaveChangesAsync();

        return ToFieldDto(field);
    }

    public async Task<SignupInputFieldDto> AdminUpdateFieldAsync(Guid signupId, Guid fieldId, UpsertSignupInputFieldRequest request)
    {
        var signup = await _db.Signups.FirstOrDefaultAsync(s => s.Id == signupId)
            ?? throw new BlueNotFoundException($"Signup '{signupId}' was not found.");
        AssertAdminAccess(signup);

        var field = await _db.SignupInputFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.SignupId == signupId)
            ?? throw new BlueNotFoundException($"Field '{fieldId}' was not found on signup '{signupId}'.");

        field.Title = request.Title;
        field.Note = request.Note;
        field.Type = request.Type;
        field.Options = request.Options;
        field.Visible = request.Visible;
        field.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync();

        return ToFieldDto(field);
    }

    public async Task AdminDeleteFieldAsync(Guid signupId, Guid fieldId)
    {
        var signup = await _db.Signups.FirstOrDefaultAsync(s => s.Id == signupId)
            ?? throw new BlueNotFoundException($"Signup '{signupId}' was not found.");
        AssertAdminAccess(signup);

        var field = await _db.SignupInputFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.SignupId == signupId)
            ?? throw new BlueNotFoundException($"Field '{fieldId}' was not found on signup '{signupId}'.");

        _db.SignupInputFields.Remove(field);
        await _db.SaveChangesAsync();
    }

    public async Task AdminReorderFieldsAsync(Guid signupId, ReorderFieldsRequest request)
    {
        var signup = await _db.Signups.FirstOrDefaultAsync(s => s.Id == signupId)
            ?? throw new BlueNotFoundException($"Signup '{signupId}' was not found.");
        AssertAdminAccess(signup);

        var fields = await _db.SignupInputFields
            .Where(f => f.SignupId == signupId)
            .ToListAsync();

        for (var i = 0; i < request.OrderedFieldIds.Count; i++)
        {
            var field = fields.FirstOrDefault(f => f.Id == request.OrderedFieldIds[i]);
            if (field != null)
                field.SortOrder = i;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<SignupResponseDto> AdminSetReservationAsync(Guid signupId, Guid responseId, SetReservationRequest request)
    {
        var signup = await LoadSignupWithDetails(signupId);
        AssertAdminAccess(signup);

        var response = signup.Responses.FirstOrDefault(r => r.Id == responseId)
            ?? throw new BlueNotFoundException($"Response '{responseId}' was not found on signup '{signupId}'.");

        response.Reservation = request.Reservation;
        await _db.SaveChangesAsync();

        var allResponses = OrderResponses(signup.Responses.ToList());
        return ToResponseDto(response, allResponses, signup.MaxSignups, signupId);
    }

    public async Task AdminDeleteResponseAsync(Guid signupId, Guid responseId)
    {
        var signup = await _db.Signups.FirstOrDefaultAsync(s => s.Id == signupId)
            ?? throw new BlueNotFoundException($"Signup '{signupId}' was not found.");
        AssertAdminAccess(signup);

        var response = await _db.SignupResponses
            .FirstOrDefaultAsync(r => r.Id == responseId && r.SignupId == signupId)
            ?? throw new BlueNotFoundException($"Response '{responseId}' was not found on signup '{signupId}'.");

        _db.SignupResponses.Remove(response);
        await _db.SaveChangesAsync();
    }

    public async Task<byte[]> AdminExportCsvAsync(Guid signupId)
    {
        var signup = await LoadSignupWithDetails(signupId);
        AssertAdminAccess(signup);

        var fields = signup.InputFields.OrderBy(f => f.SortOrder).ToList();
        var responses = OrderResponses(signup.Responses.ToList());

        var sb = new StringBuilder();

        // Header
        var headers = new List<string> { "Naam", "E-mail", "Reservering", "Status", "Aangemeld op" };
        headers.AddRange(fields.Select(f => f.Title));
        sb.AppendLine(string.Join(",", headers.Select(CsvEscape)));

        // Rows
        foreach (var response in responses)
        {
            var status = ComputeStatus(response, responses, signup.MaxSignups);
            var row = new List<string>
            {
                response.User?.Fullname ?? string.Empty,
                response.User?.Email ?? string.Empty,
                response.Reservation ? "Ja" : "Nee",
                status,
                response.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            };

            foreach (var field in fields)
            {
                var value = response.FieldValues.FirstOrDefault(v => v.FieldId == field.Id)?.Value ?? string.Empty;
                row.Add(value);
            }

            sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<HashSet<Guid>> GetAccessibleClusterIdsAsync(Guid userId)
    {
        var currentSeasonId = await GetCurrentSeasonIdAsync();

        var categoryClusterIds = await _db.MemberClusterCriteria
            .Where(c => c.Type == ClusterCriterionType.GroupCategory
                && _db.UserGroupInstanceMembers.Any(m =>
                    m.UserId == userId
                    && m.UserGroupInstance.SeasonId == currentSeasonId
                    && m.UserGroupInstance.UserGroup.UserGroupCategoryId == c.UserGroupCategoryId
                    && (c.UserGroupCategoryRoleId == null || m.UserGroupCategoryRoleId == c.UserGroupCategoryRoleId)))
            .Select(c => c.MemberClusterId)
            .ToListAsync();

        var groupClusterIds = await _db.MemberClusterCriteria
            .Where(c => c.Type == ClusterCriterionType.Group
                && _db.UserGroupInstanceMembers.Any(m =>
                    m.UserId == userId
                    && m.UserGroupInstance.SeasonId == currentSeasonId
                    && m.UserGroupInstance.UserGroupId == c.UserGroupId))
            .Select(c => c.MemberClusterId)
            .ToListAsync();

        return categoryClusterIds.Union(groupClusterIds).ToHashSet();
    }

    private async Task<Guid> GetCurrentSeasonIdAsync()
    {
        var settings = await _db.AppSettings.FirstAsync();
        return settings.CurrentSeasonId;
    }

    private async Task<Domain.Models.Signup.Signup> LoadSignupWithDetails(Guid signupId)
    {
        return await _db.Signups
            .Include(s => s.Category)
            .Include(s => s.Clusters)
            .Include(s => s.InputFields)
            .Include(s => s.Responses)
                .ThenInclude(r => r.User)
            .Include(s => s.Responses)
                .ThenInclude(r => r.FieldValues)
                    .ThenInclude(v => v.Field)
            .FirstOrDefaultAsync(s => s.Id == signupId)
            ?? throw new BlueNotFoundException($"Signup '{signupId}' was not found.");
    }

    private SignupResponse FindMyResponse(Domain.Models.Signup.Signup signup, Guid responseId)
    {
        var response = signup.Responses.FirstOrDefault(r => r.Id == responseId)
            ?? throw new BlueNotFoundException($"Response '{responseId}' was not found.");

        if (response.UserId != _currentUser.Id)
            throw new BlueNotFoundException($"Response '{responseId}' was not found.");

        return response;
    }

    private void AssertAdminAccess(Domain.Models.Signup.Signup signup)
    {
        if (!_currentUser.HasPermission(BluePermission.AdminSignupModifyOthers)
            && signup.CreatedByUserId != _currentUser.Id)
        {
            throw new BlueValidationException("You do not have permission to modify this signup.");
        }
    }

    private static List<SignupResponse> OrderResponses(List<SignupResponse> responses)
    {
        return responses
            .OrderByDescending(r => r.Reservation)
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    private static string ComputeStatus(SignupResponse response, List<SignupResponse> orderedResponses, int? maxSignups)
    {
        if (response.Reservation) return "reservation";
        if (maxSignups == null) return "valid";

        var nonReservations = orderedResponses.Where(r => !r.Reservation).ToList();
        var index = nonReservations.IndexOf(response);
        return index < maxSignups ? "valid" : "waitlist";
    }

    private static List<SignupResponseFieldValue> MapFieldValues(
        List<FieldValueRequest> fieldValues,
        IEnumerable<SignupInputField> fields)
    {
        var fieldSet = fields.Select(f => f.Id).ToHashSet();
        return fieldValues
            .Where(fv => fieldSet.Contains(fv.FieldId))
            .Select(fv => new SignupResponseFieldValue
            {
                Id = Guid.NewGuid(),
                FieldId = fv.FieldId,
                Value = fv.Value,
            })
            .ToList();
    }

    private static SignupListItemDto ToListItemDto(Domain.Models.Signup.Signup signup, Guid? currentUserId)
    {
        var responses = signup.Responses.ToList();
        var ordered = OrderResponses(responses);
        var reservations = ordered.Count(r => r.Reservation);
        var nonReservations = ordered.Where(r => !r.Reservation).ToList();
        var validCount = signup.MaxSignups.HasValue
            ? Math.Min(nonReservations.Count, signup.MaxSignups.Value)
            : nonReservations.Count;
        var waitlistCount = signup.MaxSignups.HasValue
            ? Math.Max(0, nonReservations.Count - signup.MaxSignups.Value)
            : 0;

        var myResponse = currentUserId.HasValue ? ordered.FirstOrDefault(r => r.UserId == currentUserId.Value) : null;
        var myResponseStatus = myResponse != null ? ComputeStatus(myResponse, ordered, signup.MaxSignups) : null;

        return new SignupListItemDto(
            signup.Id, signup.Title, signup.Description,
            signup.CategoryId, signup.Category?.Title,
            signup.EndDate, signup.AllowMultiple, signup.AllowDelete, signup.AllowUpdate,
            signup.MaxSignups, signup.MaxWaitlist, signup.HideSignups, signup.Anonymous,
            TotalResponses: responses.Count,
            ValidResponses: reservations + validCount,
            WaitlistCount: waitlistCount,
            HasMyResponse: myResponse != null,
            MyResponseStatus: myResponseStatus
        );
    }

    private SignupAdminDetailDto ToAdminDetailDto(Domain.Models.Signup.Signup signup)
    {
        var ordered = OrderResponses(signup.Responses.ToList());
        return new SignupAdminDetailDto(
            signup.Id, signup.Title, signup.Description,
            signup.CategoryId, signup.Category?.Title,
            signup.EndDate, signup.AllowMultiple, signup.AllowDelete, signup.AllowUpdate,
            signup.MaxSignups, signup.MaxWaitlist, signup.HideSignups, signup.Anonymous,
            signup.Clusters.Select(sc => sc.MemberClusterId).ToList(),
            signup.InputFields.OrderBy(f => f.SortOrder).Select(ToFieldDto).ToList(),
            ordered.Select(r => ToResponseDto(r, ordered, signup.MaxSignups, signup.Id)).ToList()
        );
    }

    private static SignupResponseDto ToResponseDto(
        SignupResponse response,
        List<SignupResponse> orderedResponses,
        int? maxSignups,
        Guid signupId)
    {
        return new SignupResponseDto(
            response.Id,
            signupId,
            response.UserId,
            response.User?.Fullname ?? string.Empty,
            response.Reservation,
            ComputeStatus(response, orderedResponses, maxSignups),
            response.CreatedAt,
            response.FieldValues
                .OrderBy(v => v.Field.SortOrder)
                .Select(v => new SignupResponseFieldValueDto(v.FieldId, v.Field.Title, v.Value))
                .ToList()
        );
    }

    private static SignupInputFieldDto ToFieldDto(SignupInputField f) =>
        new(f.Id, f.Title, f.Note, f.Type, f.Options, f.Visible, f.SortOrder);

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
