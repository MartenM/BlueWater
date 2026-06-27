namespace Bluewater.Core.Dto.Fleet;

public record UpsertEquipmentTypeRequest(string Code, string Name, bool Scull, bool Coxed, int RowersCount, bool IsBoat);
