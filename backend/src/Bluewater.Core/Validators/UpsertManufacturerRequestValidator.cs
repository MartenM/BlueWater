using Bluewater.Core.Dto.Fleet;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertManufacturerRequestValidator : AbstractValidator<UpsertManufacturerRequest>
{
    public UpsertManufacturerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}
