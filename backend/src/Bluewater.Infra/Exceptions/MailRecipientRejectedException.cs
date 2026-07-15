namespace Bluewater.Infra.Exceptions;

/// <summary>
/// Thrown when the SMTP server permanently rejects a recipient (e.g. mailbox unavailable/unknown
/// user). Callers in Bluewater.Core catch this to mark the recipient as bounced without needing
/// to reference MailKit types directly.
/// </summary>
public class MailRecipientRejectedException : Exception
{
    public MailRecipientRejectedException(string message)
        : base(message)
    {
    }

    public MailRecipientRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
