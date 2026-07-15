using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Services.Abstractions;

namespace Bluewater.Core.Services.Mail;

/// <summary>
/// A "dumb" Hangfire job for sending a mailing proof to the requesting admin's own email.
/// No tracking-link rewriting: a proof isn't a real recipient and would skew click/open stats.
/// </summary>
public class MailProofSendJob
{
    private readonly IMailTransportService _transportService;

    public MailProofSendJob(IMailTransportService transportService)
    {
        _transportService = transportService;
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

        await _transportService.SendAsync(envelope);
    }
}
