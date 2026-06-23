using Bluewater.Core.Dto.News;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class UpsertNewsPostRequestValidator : AbstractValidator<UpsertNewsPostRequest>
{
    public UpsertNewsPostRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortText).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AdditionalText).MaximumLength(10000);

        // A news icon can only be assigned while it's active - once soft-deleted (NewsIconDelete),
        // it drops out of this query filter and can no longer be (re)assigned to a post, even
        // though posts that already reference it keep rendering it.
        RuleFor(x => x.IconId)
            .MustAsync(async (iconId, cancellationToken) =>
                iconId is null || await db.NewsIcons.AnyAsync(x => x.Id == iconId, cancellationToken))
            .WithMessage(x => $"NewsIcon '{x.IconId}' does not exist or is no longer available.");
    }
}
