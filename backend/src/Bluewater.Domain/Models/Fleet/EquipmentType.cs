using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Fleet;

public class EquipmentType : IAuditable
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Scull { get; set; }
    public bool Coxed { get; set; }
    public int RowersCount { get; set; }
    public bool IsBoat { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
