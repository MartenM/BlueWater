using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models.Groups;

namespace Bluewater.Domain.Models.Outings;

public class OutingParticipant : IAuditableRelation
{
    public Guid OutingId { get; set; }
    public Outing Outing { get; set; } = null!;

    public Guid UserId { get; set; }
    public BlueUser User { get; set; } = null!;

    public OutingParticipantRole Role { get; set; } = OutingParticipantRole.None;
    public bool Invited { get; set; }
    public bool CheckedIn { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
