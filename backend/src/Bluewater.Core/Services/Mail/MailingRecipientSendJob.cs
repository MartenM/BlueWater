using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using Bluewater.Infra.Exceptions;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bluewater.Core.Services.Mail;

/// <summary>
/// A "dumb" Hangfire job: loads a MailingRecipient's pre-rendered content (rendered by
/// MailingService.SendAsync before enqueueing) and sends it via the configured sender.
/// </summary>
public class MailingRecipientSendJob
{
    private readonly BluewaterContext _db;
    private readonly IMailTransportService _transportService;
    private readonly ILogger<MailingRecipientSendJob> _logger;

    public MailingRecipientSendJob(BluewaterContext db, IMailTransportService transportService, ILogger<MailingRecipientSendJob> logger)
    {
        _db = db;
        _transportService = transportService;
        _logger = logger;
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

        try
        {
            await _transportService.SendAsync(envelope);
        }
        catch (MailRecipientRejectedException ex)
        {
            _logger.LogWarning(ex, "Mailing recipient {RecipientId} ({Email}) bounced for mailing {MailingId}", recipient.Id, recipient.Email, recipient.MailingId);

            recipient.Bounced = true;
            recipient.BounceReason = ex.Message;
            recipient.FailedAt = DateTime.UtcNow;

            if (recipient.UserId is not null)
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == recipient.UserId);
                if (user is not null)
                {
                    user.EmailConfirmed = false;
                }
            }

            await _db.SaveChangesAsync();
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mailing recipient {RecipientId} ({Email}) for mailing {MailingId}", recipient.Id, recipient.Email, recipient.MailingId);
            throw;
        }

        recipient.Sent = true;
        recipient.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
