using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Mail;

public class Mailing : IAuditable
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyMarkdown { get; set; } = string.Empty;
    public string SenderKey { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public MailTemplate? Template { get; set; }
    public Guid? LayoutId { get; set; }
    public MailLayout? Layout { get; set; }
    public MailingStatus Status { get; set; } = MailingStatus.Draft;
    public int ProofSendCount { get; set; }
    public DateTime? SentAt { get; set; }

    public ICollection<MailingTargetCluster> TargetClusters { get; set; } = new List<MailingTargetCluster>();
    public ICollection<MailingTargetGroupInstance> TargetGroupInstances { get; set; } = new List<MailingTargetGroupInstance>();
    public ICollection<MailingRecipient> Recipients { get; set; } = new List<MailingRecipient>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
