using Bluewater.Core.Dto.MaterialPlanner;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class SetMaterialReservationLabelRequestValidator : AbstractValidator<SetMaterialReservationLabelRequest>
{
    public SetMaterialReservationLabelRequestValidator()
    {
        RuleFor(x => x.CustomLabel).MaximumLength(200);
    }
}
