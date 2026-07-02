using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Outings;

public class OutingChangelogEntry : IAuditable
{
    public Guid Id { get; set; }

    public Guid OutingId { get; set; }
    public Outing Outing { get; set; } = null!;

    public OutingChangelogType Type { get; set; }

    /// <summary>JSON payload (stored as jsonb) with the fields needed to render a human-readable sentence for this entry.</summary>
    public string Fields { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
