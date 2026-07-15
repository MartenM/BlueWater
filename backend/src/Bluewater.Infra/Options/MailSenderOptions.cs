namespace Bluewater.Infra.Options;

public class MailSenderOptions
{
    public string Key { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string FromAddress { get; set; } = null!;
    public string? ReplyToAddress { get; set; }
    public string SmtpHost { get; set; } = null!;
    public int SmtpPort { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; }
}
