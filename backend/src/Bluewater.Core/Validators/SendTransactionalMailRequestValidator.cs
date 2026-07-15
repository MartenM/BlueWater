using Bluewater.Core.Dto.Mail;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class SendTransactionalMailRequestValidator : AbstractValidator<SendTransactionalMailRequest>
{
    public SendTransactionalMailRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.SenderKey).NotEmpty();

        RuleFor(x => x.Recipients).NotEmpty().WithMessage("At least one recipient is required.");
        RuleForEach(x => x.Recipients).ChildRules(recipient =>
        {
            recipient.RuleFor(r => r.Email).NotEmpty().EmailAddress();
        });

        RuleForEach(x => x.Cc).EmailAddress();
        RuleForEach(x => x.Bcc).EmailAddress();

        RuleFor(x => x)
            .Must(x => x.TemplateId.HasValue ^ !string.IsNullOrWhiteSpace(x.BodyMarkdownOverride))
            .WithMessage("Exactly one of TemplateId or BodyMarkdownOverride must be provided.");

        RuleFor(x => x.AttachmentStoredFileIds)
            .MustAsync(async (ids, cancellationToken) =>
            {
                if (ids.Count == 0) return true;
                var existingCount = await db.StoredFiles.CountAsync(f => ids.Contains(f.Id), cancellationToken);
                return existingCount == ids.Count;
            })
            .WithMessage("One or more attachment file ids do not exist.");
    }
}
