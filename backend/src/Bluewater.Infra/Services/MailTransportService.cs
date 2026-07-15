using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bluewater.Infra.Services;

public class MailTransportService : IMailTransportService
{
    private readonly IOptions<MailOptions> _options;

    public MailTransportService(IOptions<MailOptions> options)
    {
        _options = options;
    }

    public async Task SendAsync(MailMessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var sender = _options.Value.Senders.FirstOrDefault(s => s.Key == envelope.SenderKey)
            ?? throw new InvalidOperationException($"No mail sender configured for key '{envelope.SenderKey}'.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(sender.DisplayName, sender.FromAddress));
        message.ReplyTo.Add(new MailboxAddress(sender.DisplayName, envelope.ReplyToOverride ?? sender.ReplyToAddress ?? sender.FromAddress));
        message.To.AddRange(envelope.ToAddresses.Select(MailboxAddress.Parse));
        message.Cc.AddRange(envelope.CcAddresses.Select(MailboxAddress.Parse));
        message.Bcc.AddRange(envelope.BccAddresses.Select(MailboxAddress.Parse));
        message.Subject = envelope.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = envelope.HtmlBody,
            TextBody = envelope.PlainTextBody
        };
        foreach (var attachment in envelope.Attachments)
        {
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(sender.SmtpHost, sender.SmtpPort, sender.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        if (!string.IsNullOrEmpty(sender.Username))
        {
            await client.AuthenticateAsync(sender.Username, sender.Password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
