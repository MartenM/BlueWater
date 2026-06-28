namespace Bluewater.Core.Dto.Signup;

public record SignupListItemDto(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    string? CategoryTitle,
    DateTime? EndDate,
    bool AllowMultiple,
    bool AllowDelete,
    bool AllowUpdate,
    int? MaxSignups,
    int? MaxWaitlist,
    bool HideSignups,
    bool Anonymous,
    int TotalResponses,
    int ValidResponses,
    int WaitlistCount,
    bool HasMyResponse,
    string? MyResponseStatus);
