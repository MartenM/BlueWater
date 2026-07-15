namespace Bluewater.Core.Dto.Mail;

public record MailLayoutDto(
    Guid Id,
    string Name,
    string HeaderHtml,
    string FooterHtml,
    bool IsDefault);
