using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Mail;

public class MailingRecipient : IAuditable
{
    public Guid Id { get; set; }

    public Guid MailingId { get; set; }
    public Mailing Mailing { get; set; } = null!;

    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public bool Sent { get; set; }
    public DateTime? SentAt { get; set; }

    public bool Opened { get; set; }
    public DateTime? FirstOpenedAt { get; set; }
    public int OpenCount { get; set; }

    public string TrackingToken { get; set; } = string.Empty;

    public string? RenderedSubject { get; set; }
    public string? RenderedHtmlBody { get; set; }
    public string? RenderedPlainTextBody { get; set; }

    public ICollection<MailingRecipientLink> Links { get; set; } = new List<MailingRecipientLink>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
