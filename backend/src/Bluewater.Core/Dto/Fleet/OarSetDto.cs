namespace Bluewater.Core.Dto.Fleet;

public record OarSetDto(Guid Id, string Name, Guid? ManufacturerId, string? ManufacturerName, bool Scull);
