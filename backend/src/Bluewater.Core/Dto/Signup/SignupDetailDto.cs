namespace Bluewater.Core.Dto.Signup;

public record SignupDetailDto(
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
    List<SignupInputFieldDto> Fields,
    List<SignupResponsePublicDto>? Responses,
    SignupResponseDto? MyResponse);

public record SignupAdminDetailDto(
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
    List<Guid> ClusterIds,
    List<SignupInputFieldDto> Fields,
    List<SignupResponseDto> Responses);

public record UpsertSignupRequest(
    string Title,
    string? Description,
    Guid CategoryId,
    DateTime? EndDate,
    bool AllowMultiple,
    bool AllowDelete,
    bool AllowUpdate,
    int? MaxSignups,
    int? MaxWaitlist,
    bool HideSignups,
    bool Anonymous,
    List<Guid> ClusterIds);
