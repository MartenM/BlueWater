namespace Bluewater.Core.Dto.Mail;

public record UpsertMailLayoutRequest(
    string Name,
    string HeaderHtml,
    string FooterHtml,
    bool IsDefault);
