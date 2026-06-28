using Bluewater.Domain.Models.Signup;

namespace Bluewater.Core.Dto.Signup;

public record SignupInputFieldDto(
    Guid Id,
    string Title,
    string? Note,
    SignupInputFieldType Type,
    string? Options,
    bool Visible,
    int SortOrder);

public record UpsertSignupInputFieldRequest(
    string Title,
    string? Note,
    SignupInputFieldType Type,
    string? Options,
    bool Visible,
    int SortOrder);

public record ReorderFieldsRequest(List<Guid> OrderedFieldIds);
