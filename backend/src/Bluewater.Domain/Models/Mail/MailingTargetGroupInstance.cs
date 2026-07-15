using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models.Groups;

namespace Bluewater.Domain.Models.Mail;

public class MailingTargetGroupInstance : IAuditableRelation
{
    public Guid MailingId { get; set; }
    public Mailing Mailing { get; set; } = null!;

    public Guid UserGroupInstanceId { get; set; }
    public UserGroupInstance UserGroupInstance { get; set; } = null!;

    public DateTime? LastSentAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
