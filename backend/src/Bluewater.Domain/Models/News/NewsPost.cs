using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.News;

public class NewsPost : IAuditable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortText { get; set; } = string.Empty;
    public string? AdditionalText { get; set; }
    public bool MembersOnly { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
