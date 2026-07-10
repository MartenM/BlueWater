using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models.Fleet;

namespace Bluewater.Domain.Models.MaterialPlanner;

public class MaterialReservation : IAuditable
{
    public Guid Id { get; set; }

    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string? CustomLabel { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
