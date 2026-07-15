using Bluewater.Core.Services.Abstractions;
using Bluewater.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class MailTrackingService : IMailTrackingService
{
    private readonly BluewaterContext _db;

    public MailTrackingService(BluewaterContext db)
    {
        _db = db;
    }

    public async Task<string?> RecordClickAsync(string token)
    {
        var link = await _db.MailingRecipientLinks.FirstOrDefaultAsync(x => x.Token == token);
        if (link is null)
            return null;

        if (link.FirstClickedAt is null)
            link.FirstClickedAt = DateTime.UtcNow;
        link.ClickCount++;
        await _db.SaveChangesAsync();

        return link.OriginalUrl;
    }

    public async Task RecordOpenAsync(string token)
    {
        var recipient = await _db.MailingRecipients.FirstOrDefaultAsync(x => x.TrackingToken == token);
        if (recipient is null)
            return;

        if (recipient.FirstOpenedAt is null)
            recipient.FirstOpenedAt = DateTime.UtcNow;
        recipient.Opened = true;
        recipient.OpenCount++;
        await _db.SaveChangesAsync();
    }
}
