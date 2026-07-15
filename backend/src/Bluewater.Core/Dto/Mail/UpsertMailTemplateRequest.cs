using Bluewater.Domain.Models.Mail;

namespace Bluewater.Core.Dto.Mail;

public record UpsertMailTemplateRequest(
    string Name,
    MailTemplateKind Kind,
    string SubjectTemplate,
    string BodyMarkdown,
    Guid? DefaultLayoutId,
    string? DefaultSenderKey);
