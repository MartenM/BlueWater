namespace Bluewater.Core.Services.Mail;

public interface IMailContentRenderer
{
    /// <summary>
    /// Renders a (subject template, body markdown, layout header/footer) triple against a
    /// merge-token context. Substitutes tokens into the markdown source first, converts to
    /// HTML, then wraps with the layout (whose header/footer may also contain tokens).
    /// </summary>
    RenderedMailContent Render(string subjectTemplate, string bodyMarkdown, string? headerHtml, string? footerHtml, MergeTokenContext context);
}
