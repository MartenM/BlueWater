namespace Bluewater.Infra.Options;

public class MailOptions
{
    public List<MailSenderOptions> Senders { get; set; } = [];
    public string? PublicBaseUrl { get; set; }
    public int SendDelayMilliseconds { get; set; } = 1000;
}
