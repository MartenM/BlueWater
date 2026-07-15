using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Mail;

public class MailingRecipientLink : IAuditable
{
    public Guid Id { get; set; }

    public Guid MailingRecipientId { get; set; }
    public MailingRecipient MailingRecipient { get; set; } = null!;

    public string OriginalUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int ClickCount { get; set; }
    public DateTime? FirstClickedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
