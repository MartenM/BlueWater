namespace Bluewater.Core.Dto.Mail;

/// <summary>Optional overrides so a not-yet-saved edit can be previewed; falls back to the persisted template when omitted.</summary>
public record MailTemplatePreviewRequest(
    string? SubjectTemplate,
    string? BodyMarkdown,
    Guid? LayoutId);
