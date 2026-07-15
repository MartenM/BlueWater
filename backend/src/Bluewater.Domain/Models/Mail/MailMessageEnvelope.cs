namespace Bluewater.Domain.Models.Mail;

public class MailMessageEnvelope
{
    public string SenderKey { get; set; } = null!;
    public List<string> ToAddresses { get; set; } = [];
    public List<string> CcAddresses { get; set; } = [];
    public List<string> BccAddresses { get; set; } = [];
    public string? ReplyToOverride { get; set; }
    public string Subject { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
    public string? PlainTextBody { get; set; }
    public List<MailAttachment> Attachments { get; set; } = [];
}
