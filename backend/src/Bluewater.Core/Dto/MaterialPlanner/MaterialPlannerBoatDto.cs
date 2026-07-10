namespace Bluewater.Core.Dto.MaterialPlanner;

public record MaterialPlannerBoatDto(Guid EquipmentId, string Name, List<MaterialReservationDto> Reservations);
