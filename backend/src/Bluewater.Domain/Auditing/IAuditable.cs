namespace Bluewater.Domain.Auditing;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid CreatedByUserId { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedByUserId { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedByUserId { get; set; }
}
