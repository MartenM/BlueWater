using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Fleet;

public class Equipment : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? EquipmentTypeId { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public Guid? OarSetId { get; set; }
    public OarSet? OarSet { get; set; }
    public bool FreeFleet { get; set; }
    public bool OutOfOrder { get; set; }
    public int? RowersWeight { get; set; }
    public int? RowersWeightMax { get; set; }
    public DateOnly? DateBuild { get; set; }
    public DateOnly? DateBought { get; set; }
    public DateOnly? DateSold { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
