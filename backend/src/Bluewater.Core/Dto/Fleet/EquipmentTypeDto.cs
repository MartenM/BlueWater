namespace Bluewater.Core.Dto.Fleet;

public record EquipmentTypeDto(Guid Id, string Code, string Name, bool Scull, bool Coxed, int RowersCount, bool IsBoat);
