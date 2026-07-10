namespace Bluewater.Core.Dto.MaterialPlanner;

public record MaterialPlannerBoatTypeGroupDto(Guid? EquipmentTypeId, string TypeLabel, List<MaterialPlannerBoatDto> Boats);
