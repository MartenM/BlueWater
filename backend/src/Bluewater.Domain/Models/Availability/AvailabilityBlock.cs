using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models;

namespace Bluewater.Domain.Models.Availability;

public class AvailabilityBlock : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public BlueUser User { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
