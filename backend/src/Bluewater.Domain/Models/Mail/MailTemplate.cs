using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Mail;

public class MailTemplate : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MailTemplateKind Kind { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyMarkdown { get; set; } = string.Empty;
    public Guid? DefaultLayoutId { get; set; }
    public MailLayout? DefaultLayout { get; set; }
    public string? DefaultSenderKey { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
