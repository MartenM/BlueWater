namespace Bluewater.Domain.Models.Mail;

public class MailAttachment
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public byte[] Content { get; set; } = null!;
}
