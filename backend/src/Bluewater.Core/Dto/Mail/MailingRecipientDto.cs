namespace Bluewater.Core.Dto.Mail;

public record MailingRecipientDto(
    Guid Id,
    Guid? UserId,
    string Email,
    string FullName,
    bool Sent,
    DateTime? SentAt,
    bool Bounced,
    string? BounceReason,
    bool Opened,
    DateTime? FirstOpenedAt,
    int OpenCount);
