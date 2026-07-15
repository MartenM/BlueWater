using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Exceptions;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Bluewater.Core.Services.Mail;

/// <summary>
/// A "dumb" Hangfire job for sending a mailing proof to the requesting admin's own email.
/// No tracking-link rewriting: a proof isn't a real recipient and would skew click/open stats.
/// </summary>
public class MailProofSendJob
{
    private readonly IMailTransportService _transportService;
    private readonly ILogger<MailProofSendJob> _logger;

    public MailProofSendJob(IMailTransportService transportService, ILogger<MailProofSendJob> logger)
    {
        _transportService = transportService;
        _logger = logger;
    }

    public async Task ExecuteAsync(string senderKey, string toAddress, string subject, string htmlBody, string? plainTextBody)
    {
        var envelope = new MailMessageEnvelope
        {
            SenderKey = senderKey,
            ToAddresses = [toAddress],
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
        };

        try
        {
            await _transportService.SendAsync(envelope);
        }
        catch (MailRecipientRejectedException ex)
        {
            _logger.LogWarning(ex, "Proof mail rejected for {ToAddress}", toAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send proof mail to {ToAddress}", toAddress);
            throw;
        }
    }
}
