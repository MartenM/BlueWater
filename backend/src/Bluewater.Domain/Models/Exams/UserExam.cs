using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Exams;

public class UserExam : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public BlueUser User { get; set; } = null!;
    public Guid ExamTypeId { get; set; }
    public ExamType ExamType { get; set; } = null!;
    public DateOnly ObtainedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
