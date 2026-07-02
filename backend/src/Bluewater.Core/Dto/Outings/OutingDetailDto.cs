namespace Bluewater.Core.Dto.Outings;

public record OutingDetailDto(
    Guid Id,
    Guid UserGroupInstanceId,
    string UserGroupInstanceName,
    DateTime OutingDate,
    DateTime? OutingDateEnd,
    Guid? BoatTypeId,
    string? BoatTypeName,
    string? BoatTypeDifferent,
    int? RowerCapacity,
    bool CoxRequired,
    Guid? BoatId,
    string? BoatName,
    string? Description,
    bool Confirmed,
    IReadOnlyList<OutingParticipantDto> Participants);
