namespace Bluewater.Core.Dto.MaterialPlanner;

public record MaterialReservationDto(
    Guid Id,
    Guid EquipmentId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid OwnerUserId,
    string OwnerFullname,
    string? CustomLabel,
    bool CanEdit,
    Guid? OutingId);
