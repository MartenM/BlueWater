namespace Bluewater.Core.Dto.Signup;

public record SignupResponseFieldValueDto(Guid FieldId, string FieldTitle, string? Value);

public record SignupResponsePublicDto(
    Guid Id,
    Guid? UserId,
    string? UserFullname,
    bool Reservation,
    string Status,
    DateTime CreatedAt,
    List<SignupResponseFieldValueDto> FieldValues);

public record SignupResponseDto(
    Guid Id,
    Guid SignupId,
    Guid UserId,
    string UserFullname,
    bool Reservation,
    string Status,
    DateTime CreatedAt,
    List<SignupResponseFieldValueDto> FieldValues);

public record SubmitResponseRequest(List<FieldValueRequest> FieldValues);

public record UpdateResponseRequest(List<FieldValueRequest> FieldValues);

public record FieldValueRequest(Guid FieldId, string? Value);

public record SetReservationRequest(bool Reservation);
