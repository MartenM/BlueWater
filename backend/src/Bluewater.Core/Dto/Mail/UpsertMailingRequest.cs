namespace Bluewater.Core.Dto.Mail;

public record UpsertMailingRequest(
    string Subject,
    string BodyMarkdown,
    string SenderKey,
    Guid? TemplateId,
    Guid? LayoutId);
