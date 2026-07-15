using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services.Mail;

/// <summary>
/// A "dumb" Hangfire job: loads a MailingRecipient's pre-rendered content (rendered by
/// MailingService.SendAsync before enqueueing) and sends it via the configured sender.
/// </summary>
public class MailingRecipientSendJob
{
    private readonly BluewaterContext _db;
    private readonly IMailTransportService _transportService;

    public MailingRecipientSendJob(BluewaterContext db, IMailTransportService transportService)
    {
        _db = db;
        _transportService = transportService;
    }

    public async Task ExecuteAsync(Guid mailingRecipientId)
    {
        var recipient = await _db.MailingRecipients
            .Include(x => x.Mailing)
            .FirstOrDefaultAsync(x => x.Id == mailingRecipientId);

        if (recipient is null || recipient.Sent)
            return;

        var envelope = new MailMessageEnvelope
        {
            SenderKey = recipient.Mailing.SenderKey,
            ToAddresses = [recipient.Email],
            Subject = recipient.RenderedSubject ?? string.Empty,
            HtmlBody = recipient.RenderedHtmlBody ?? string.Empty,
            PlainTextBody = recipient.RenderedPlainTextBody,
        };

        await _transportService.SendAsync(envelope);

        recipient.Sent = true;
        recipient.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
