using Bluewater.Core.Dto.Fleet;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertOarSetRequestValidator : AbstractValidator<UpsertOarSetRequest>
{
    public UpsertOarSetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}
