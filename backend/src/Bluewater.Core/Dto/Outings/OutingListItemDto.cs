using Bluewater.Domain.Models.Outings;

namespace Bluewater.Core.Dto.Outings;

public record OutingListItemDto(
    Guid Id,
    Guid UserGroupInstanceId,
    string UserGroupInstanceName,
    DateTime OutingDate,
    DateTime? OutingDateEnd,
    Guid? BoatTypeId,
    string? BoatTypeName,
    string? BoatTypeDifferent,
    Guid? BoatId,
    string? BoatName,
    bool Confirmed,
    int RowerCount,
    int? RowerCapacity,
    bool CoxAssigned,
    bool CoxRequired,
    OutingParticipantRole? MyRole,
    bool MyCheckedIn);
