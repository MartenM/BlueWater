using Bluewater.Domain.Models.Mail;

namespace Bluewater.Core.Dto.Mail;

public record MailTemplateDto(
    Guid Id,
    string Name,
    MailTemplateKind Kind,
    string SubjectTemplate,
    string BodyMarkdown,
    Guid? DefaultLayoutId,
    string? DefaultSenderKey);
