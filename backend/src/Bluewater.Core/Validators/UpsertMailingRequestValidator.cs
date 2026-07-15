using Bluewater.Core.Dto.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class UpsertMailingRequestValidator : AbstractValidator<UpsertMailingRequest>
{
    public UpsertMailingRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BodyMarkdown).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.SenderKey).NotEmpty().MaximumLength(100);

        RuleFor(x => x.TemplateId)
            .MustAsync(async (templateId, cancellationToken) =>
                templateId is null || await db.MailTemplates.AnyAsync(x => x.Id == templateId, cancellationToken))
            .WithMessage(x => $"MailTemplate '{x.TemplateId}' does not exist.");

        RuleFor(x => x.LayoutId)
            .MustAsync(async (layoutId, cancellationToken) =>
                layoutId is null || await db.MailLayouts.AnyAsync(x => x.Id == layoutId, cancellationToken))
            .WithMessage(x => $"MailLayout '{x.LayoutId}' does not exist.");
    }
}
