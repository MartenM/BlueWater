using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Bluewater.Core.Services.Mail;

public class MailContentRenderer : IMailContentRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    private readonly IMergeTokenRenderer _tokenRenderer;

    public MailContentRenderer(IMergeTokenRenderer tokenRenderer)
    {
        _tokenRenderer = tokenRenderer;
    }

    public RenderedMailContent Render(string subjectTemplate, string bodyMarkdown, string? headerHtml, string? footerHtml, MergeTokenContext context)
    {
        var subject = _tokenRenderer.Render(subjectTemplate, context);
        var markdownWithTokens = _tokenRenderer.Render(bodyMarkdown, context);

        var bodyHtml = Markdown.ToHtml(markdownWithTokens, Pipeline);
        var header = _tokenRenderer.Render(headerHtml ?? string.Empty, context);
        var footer = _tokenRenderer.Render(footerHtml ?? string.Empty, context);
        var htmlBody = header + bodyHtml + footer;

        var plainTextBody = ExtractPlainText(markdownWithTokens);

        return new RenderedMailContent(subject, htmlBody, plainTextBody);
    }

    private static string ExtractPlainText(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        var builder = new StringBuilder();
        AppendBlocks(document, builder);
        return builder.ToString().Trim();
    }

    private static void AppendBlocks(ContainerBlock container, StringBuilder builder)
    {
        foreach (var block in container)
        {
            switch (block)
            {
                case LeafBlock leaf when leaf.Inline is not null:
                    AppendInlines(leaf.Inline, builder);
                    builder.AppendLine();
                    builder.AppendLine();
                    break;
                case ContainerBlock nested:
                    AppendBlocks(nested, builder);
                    break;
            }
        }
    }

    private static void AppendInlines(ContainerInline container, StringBuilder builder)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case LineBreakInline:
                    builder.AppendLine();
                    break;
                case ContainerInline nested:
                    AppendInlines(nested, builder);
                    break;
            }
        }
    }
}
