namespace Bluewater.Core.Dto.MaterialPlanner;

public record CreateMaterialReservationRequest(Guid EquipmentId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
