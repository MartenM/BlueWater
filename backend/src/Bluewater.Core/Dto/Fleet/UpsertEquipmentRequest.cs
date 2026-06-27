namespace Bluewater.Core.Dto.Fleet;

public record UpsertEquipmentRequest(
    string Name,
    string? Description,
    Guid? EquipmentTypeId,
    Guid? ManufacturerId,
    Guid? OarSetId,
    Guid? RequiredExamTypeId,
    bool FreeFleet,
    bool OutOfOrder,
    int? RowersWeight,
    int? RowersWeightMax,
    DateOnly? DateBuild,
    DateOnly? DateBought,
    DateOnly? DateSold);
