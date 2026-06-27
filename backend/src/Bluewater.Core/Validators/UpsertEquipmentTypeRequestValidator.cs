using Bluewater.Core.Dto.Fleet;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertEquipmentTypeRequestValidator : AbstractValidator<UpsertEquipmentTypeRequest>
{
    public UpsertEquipmentTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MinimumLength(1).MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.RowersCount).GreaterThanOrEqualTo(0);
    }
}
