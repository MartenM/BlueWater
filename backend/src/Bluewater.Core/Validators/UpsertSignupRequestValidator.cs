using Bluewater.Core.Dto.Signup;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class UpsertSignupRequestValidator : AbstractValidator<UpsertSignupRequest>
{
    public UpsertSignupRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
        RuleFor(x => x.MaxSignups).GreaterThan(0).When(x => x.MaxSignups.HasValue);
        RuleFor(x => x.MaxWaitlist).GreaterThan(0).When(x => x.MaxWaitlist.HasValue);
        RuleFor(x => x.ClusterIds).NotEmpty().WithMessage("At least one cluster must be assigned.");
        RuleForEach(x => x.ClusterIds)
            .MustAsync(async (id, ct) => await db.MemberClusters.AnyAsync(c => c.Id == id, ct))
            .WithMessage((_, id) => $"Cluster '{id}' does not exist.");
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A category is required.")
            .MustAsync(async (id, ct) => await db.SignupCategories.AnyAsync(c => c.Id == id, ct))
            .WithMessage("Category does not exist.");
    }
}
