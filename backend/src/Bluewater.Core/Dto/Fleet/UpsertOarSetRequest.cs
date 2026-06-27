namespace Bluewater.Core.Dto.Fleet;

public record UpsertOarSetRequest(string Name, Guid? ManufacturerId, bool Scull);
