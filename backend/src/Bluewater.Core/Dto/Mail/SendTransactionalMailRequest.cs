namespace Bluewater.Core.Dto.Mail;

public record TransactionalRecipient(
    string Email,
    string? DisplayName,
    IReadOnlyDictionary<string, string> MergeValues,
    Guid? UserId);

public record SendTransactionalMailRequest(
    Guid? TemplateId,
    string? SubjectOverride,
    string? BodyMarkdownOverride,
    string SenderKey,
    List<TransactionalRecipient> Recipients,
    List<string> Cc,
    List<string> Bcc,
    string? ReplyToOverride,
    List<Guid> AttachmentStoredFileIds);
