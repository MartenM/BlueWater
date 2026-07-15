using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Services.Abstractions;

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

    public TransactionalMailJob(IMailTransportService transportService, IFileStorageService fileStorageService)
    {
        _transportService = transportService;
        _fileStorageService = fileStorageService;
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
        List<Guid> attachmentStoredFileIds)
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

        await _transportService.SendAsync(envelope);
    }
}
