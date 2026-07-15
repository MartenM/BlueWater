using Bluewater.Core.Dto.Mail;
using Bluewater.Core.Exceptions;
using Bluewater.Core.Services.Abstractions;
using Bluewater.Core.Services.Mail;
using Bluewater.Domain.Models.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Services;

public class MailTemplateService : IMailTemplateService
{
    private static readonly IReadOnlyDictionary<string, string> BaseSampleTokenValues =
        MailPlaceholderCatalog.Base.ToDictionary(x => x.Token, x => x.SampleValue, StringComparer.OrdinalIgnoreCase);

    private readonly BluewaterContext _db;
    private readonly IValidator<UpsertMailTemplateRequest> _validator;
    private readonly IMailContentRenderer _renderer;

    public MailTemplateService(BluewaterContext db, IValidator<UpsertMailTemplateRequest> validator, IMailContentRenderer renderer)
    {
        _db = db;
        _validator = validator;
        _renderer = renderer;
    }

    public async Task<List<MailTemplateDto>> ListAsync(MailTemplateKind? kind = null)
    {
        var query = _db.MailTemplates.AsQueryable();
        if (kind is not null)
            query = query.Where(x => x.Kind == kind);

        return await query
            .OrderBy(x => x.Name)
            .Select(ToDtoExpression)
            .ToListAsync();
    }

    public async Task<MailTemplateDto> GetAsync(Guid id)
    {
        return ToDto(await Find(id));
    }

    public async Task<MailTemplateDto> CreateAsync(UpsertMailTemplateRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        if (request.Kind == MailTemplateKind.Transactional)
        {
            throw new BlueValidationException(
                "Transactional mail templates are seeded automatically and cannot be created manually.");
        }

        var template = new MailTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Kind = request.Kind,
            SubjectTemplate = request.SubjectTemplate,
            BodyMarkdown = request.BodyMarkdown,
            DefaultLayoutId = request.DefaultLayoutId,
            DefaultSenderKey = request.DefaultSenderKey,
        };

        _db.MailTemplates.Add(template);
        await _db.SaveChangesAsync();

        return ToDto(template);
    }

    public async Task<MailTemplateDto> UpdateAsync(Guid id, UpsertMailTemplateRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);
        var template = await Find(id);

        if (request.Kind != template.Kind)
        {
            throw new BlueValidationException("A mail template's kind cannot be changed after creation.");
        }

        if (template.Kind == MailTemplateKind.Transactional && !string.Equals(request.Name, template.Name, StringComparison.Ordinal))
        {
            throw new BlueValidationException(
                "Transactional mail template names cannot be changed: they are looked up by exact name in code.");
        }

        template.Name = request.Name;
        template.SubjectTemplate = request.SubjectTemplate;
        template.BodyMarkdown = request.BodyMarkdown;
        template.DefaultLayoutId = request.DefaultLayoutId;
        template.DefaultSenderKey = request.DefaultSenderKey;

        await _db.SaveChangesAsync();

        return ToDto(template);
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await Find(id);

        if (template.Kind == MailTemplateKind.Transactional)
        {
            throw new BlueValidationException("Transactional mail templates cannot be deleted.");
        }

        _db.MailTemplates.Remove(template);
        await _db.SaveChangesAsync();
    }

    public async Task<MailTemplatePreviewDto> PreviewAsync(Guid id, MailTemplatePreviewRequest request)
    {
        var template = await Find(id);

        var layoutId = request.LayoutId ?? template.DefaultLayoutId;
        var layout = layoutId is null ? null : await _db.MailLayouts.FirstOrDefaultAsync(x => x.Id == layoutId);

        var context = new MergeTokenContext(SampleTokenValuesFor(template));
        var rendered = _renderer.Render(
            request.SubjectTemplate ?? template.SubjectTemplate,
            request.BodyMarkdown ?? template.BodyMarkdown,
            layout?.HeaderHtml,
            layout?.FooterHtml,
            context);

        return new MailTemplatePreviewDto(rendered.Subject, rendered.HtmlBody, rendered.PlainTextBody);
    }

    public async Task<List<MailPlaceholderDto>> GetPlaceholdersAsync(Guid? templateId)
    {
        var placeholders = MailPlaceholderCatalog.Base
            .Select(x => new MailPlaceholderDto(x.Token, x.Description))
            .ToList();

        if (templateId is null)
        {
            return placeholders;
        }

        var template = await Find(templateId.Value);
        var definition = FindTransactionalDefinition(template);
        if (definition is not null)
        {
            placeholders.AddRange(definition.ExtraPlaceholders.Select(x => new MailPlaceholderDto(x.Token, x.Description)));
        }

        return placeholders;
    }

    private static RequiredTransactionalMailTemplateDefinition? FindTransactionalDefinition(MailTemplate template) =>
        template.Kind == MailTemplateKind.Transactional
            ? RequiredTransactionalMailTemplates.All.FirstOrDefault(x => x.Name == template.Name)
            : null;

    private static IReadOnlyDictionary<string, string> SampleTokenValuesFor(MailTemplate template)
    {
        var definition = FindTransactionalDefinition(template);
        if (definition is null || definition.ExtraPlaceholders.Count == 0)
        {
            return BaseSampleTokenValues;
        }

        var values = new Dictionary<string, string>(BaseSampleTokenValues, StringComparer.OrdinalIgnoreCase);
        foreach (var extra in definition.ExtraPlaceholders)
        {
            values[extra.Token] = extra.SampleValue;
        }

        return values;
    }

    private async Task<MailTemplate> Find(Guid id)
    {
        return await _db.MailTemplates.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BlueNotFoundException($"MailTemplate '{id}' was not found.");
    }

    private static readonly System.Linq.Expressions.Expression<Func<MailTemplate, MailTemplateDto>> ToDtoExpression =
        x => new MailTemplateDto(x.Id, x.Name, x.Kind, x.SubjectTemplate, x.BodyMarkdown, x.DefaultLayoutId, x.DefaultSenderKey);

    private static MailTemplateDto ToDto(MailTemplate template) =>
        new(template.Id, template.Name, template.Kind, template.SubjectTemplate, template.BodyMarkdown, template.DefaultLayoutId, template.DefaultSenderKey);
}
