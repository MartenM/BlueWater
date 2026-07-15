namespace Bluewater.Core.Dto.Mail;

public record MailingPreviewDto(string Subject, string HtmlBody, string? PlainTextBody);
