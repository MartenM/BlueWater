using Bluewater.Core.Dto.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class UpsertMailTemplateRequestValidator : AbstractValidator<UpsertMailTemplateRequest>
{
    public UpsertMailTemplateRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SubjectTemplate).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BodyMarkdown).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.DefaultSenderKey).MaximumLength(100);

        RuleFor(x => x.DefaultLayoutId)
            .MustAsync(async (layoutId, cancellationToken) =>
                layoutId is null || await db.MailLayouts.AnyAsync(x => x.Id == layoutId, cancellationToken))
            .WithMessage(x => $"MailLayout '{x.DefaultLayoutId}' does not exist.");
    }
}
