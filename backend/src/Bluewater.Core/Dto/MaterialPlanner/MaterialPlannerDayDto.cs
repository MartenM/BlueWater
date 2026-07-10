namespace Bluewater.Core.Dto.MaterialPlanner;

public record MaterialPlannerDayDto(DateOnly Date, List<MaterialPlannerBoatTypeGroupDto> BoatTypeGroups);
