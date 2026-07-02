namespace Bluewater.Core.Dto.Outings;

public record UpsertOutingRequest(
    Guid UserGroupInstanceId,
    DateTime OutingDate,
    DateTime? OutingDateEnd,
    Guid? BoatTypeId,
    string? BoatTypeDifferent,
    Guid? BoatId,
    string? Description);
