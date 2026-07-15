using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Exceptions;
using Bluewater.Infra.Options;
using Bluewater.Infra.Services.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bluewater.Infra.Services;

public class MailTransportService : IMailTransportService
{
    private readonly IOptions<MailOptions> _options;
    private readonly ILogger<MailTransportService> _logger;

    public MailTransportService(IOptions<MailOptions> options, ILogger<MailTransportService> logger)
    {
        _options = options;
        _logger = logger;
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

        var recipients = string.Join(", ", envelope.ToAddresses);

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(sender.SmtpHost, sender.SmtpPort, sender.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
            if (!string.IsNullOrEmpty(sender.Username))
            {
                await client.AuthenticateAsync(sender.Username, sender.Password, cancellationToken);
            }
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
        {
            _logger.LogWarning(ex, "Recipient permanently rejected (550) by SMTP server via sender {SenderKey} for {Recipients}: {StatusCode} {Message}",
                envelope.SenderKey, recipients, ex.StatusCode, ex.Message);
            throw new MailRecipientRejectedException($"Recipient(s) {recipients} rejected: {ex.Message}", ex);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogWarning(ex, "Transient SMTP rejection via sender {SenderKey} for {Recipients}: {StatusCode} {Message}",
                envelope.SenderKey, recipients, ex.StatusCode, ex.Message);
            _logger.LogError(ex, "Failed to send mail via sender {SenderKey} to {Recipients}", envelope.SenderKey, recipients);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mail via sender {SenderKey} to {Recipients}", envelope.SenderKey, recipients);
            throw;
        }
    }
}
