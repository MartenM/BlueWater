using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using Bluewater.Infra.Exceptions;
using Bluewater.Infra.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bluewater.Core.Services.Mail;

/// <summary>
/// A "dumb" Hangfire job: given already-rendered content, it just loads attachment bytes and
/// sends. Rendering happens in <see cref="MailService"/> before enqueueing, since Hangfire jobs
/// execute later against a fresh scope and shouldn't need live template/layout data.
/// </summary>
public class TransactionalMailJob
{
    private readonly IMailTransportService _transportService;
    private readonly IFileStorageService _fileStorageService;
    private readonly BluewaterContext _db;
    private readonly ILogger<TransactionalMailJob> _logger;

    public TransactionalMailJob(IMailTransportService transportService, IFileStorageService fileStorageService, BluewaterContext db, ILogger<TransactionalMailJob> logger)
    {
        _transportService = transportService;
        _fileStorageService = fileStorageService;
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        string senderKey,
        List<string> toAddresses,
        List<string> ccAddresses,
        List<string> bccAddresses,
        string? replyToOverride,
        string subject,
        string htmlBody,
        string? plainTextBody,
        List<Guid> attachmentStoredFileIds,
        Guid? userId)
    {
        var envelope = new MailMessageEnvelope
        {
            SenderKey = senderKey,
            ToAddresses = toAddresses,
            CcAddresses = ccAddresses,
            BccAddresses = bccAddresses,
            ReplyToOverride = replyToOverride,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
        };

        foreach (var fileId in attachmentStoredFileIds)
        {
            var (metadata, content) = await _fileStorageService.RetrieveAsync(fileId);
            using var stream = content;
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            envelope.Attachments.Add(new MailAttachment
            {
                FileName = metadata.OriginalFileName,
                ContentType = metadata.ContentType,
                Content = memoryStream.ToArray(),
            });
        }

        try
        {
            await _transportService.SendAsync(envelope);
        }
        catch (MailRecipientRejectedException ex)
        {
            _logger.LogWarning(ex, "Transactional mail rejected for {Recipients}", string.Join(", ", toAddresses));

            if (userId is not null)
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
                if (user is not null)
                {
                    user.EmailConfirmed = false;
                    await _db.SaveChangesAsync();
                }
            }
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transactional mail to {Recipients}", string.Join(", ", toAddresses));
            throw;
        }
    }
}
