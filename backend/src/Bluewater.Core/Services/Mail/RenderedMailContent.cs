namespace Bluewater.Core.Services.Mail;

public record RenderedMailContent(string Subject, string HtmlBody, string PlainTextBody);
