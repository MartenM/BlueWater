namespace Bluewater.Core.Dto.Mail;

public record MailTemplatePreviewDto(
    string Subject,
    string HtmlBody,
    string PlainTextBody);
