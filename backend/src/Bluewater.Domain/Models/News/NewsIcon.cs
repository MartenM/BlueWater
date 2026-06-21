using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.News;

public class NewsIcon : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid FileId { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
