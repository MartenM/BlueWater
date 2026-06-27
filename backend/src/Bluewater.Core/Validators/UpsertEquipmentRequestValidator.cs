using Bluewater.Core.Dto.Fleet;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertEquipmentRequestValidator : AbstractValidator<UpsertEquipmentRequest>
{
    public UpsertEquipmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.RowersWeight).GreaterThan(0).When(x => x.RowersWeight.HasValue);
        RuleFor(x => x.RowersWeightMax).GreaterThan(0).When(x => x.RowersWeightMax.HasValue);
    }
}
