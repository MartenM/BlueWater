namespace Bluewater.Core.Dto.Fleet;

public record EquipmentDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? EquipmentTypeId,
    string? EquipmentTypeName,
    Guid? ManufacturerId,
    string? ManufacturerName,
    Guid? OarSetId,
    string? OarSetName,
    Guid? RequiredExamTypeId,
    string? RequiredExamTypeName,
    bool FreeFleet,
    bool OutOfOrder,
    int? RowersWeight,
    int? RowersWeightMax,
    DateOnly? DateBuild,
    DateOnly? DateBought,
    DateOnly? DateSold);
