namespace Bluewater.Core.Dto.MaterialPlanner;

public record MaterialReservationConflictDto(bool HasConflict, MaterialReservationDto? ConflictingReservation);
