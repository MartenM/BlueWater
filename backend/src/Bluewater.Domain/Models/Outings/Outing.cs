using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models.Fleet;
using Bluewater.Domain.Models.Groups;

namespace Bluewater.Domain.Models.Outings;

public class Outing : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserGroupInstanceId { get; set; }
    public UserGroupInstance UserGroupInstance { get; set; } = null!;

    public DateTime OutingDate { get; set; }
    public DateTime? OutingDateEnd { get; set; }

    public Guid? BoatTypeId { get; set; }
    public EquipmentType? BoatType { get; set; }
    public string? BoatTypeDifferent { get; set; }

    public Guid? BoatId { get; set; }
    public Equipment? Boat { get; set; }

    public string? Description { get; set; }

    public bool Confirmed { get; set; }

    public ICollection<OutingParticipant> Participants { get; set; } = new List<OutingParticipant>();
    public ICollection<OutingChangelogEntry> ChangelogEntries { get; set; } = new List<OutingChangelogEntry>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
