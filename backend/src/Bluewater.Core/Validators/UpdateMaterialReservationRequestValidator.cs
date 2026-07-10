using Bluewater.Core.Dto.MaterialPlanner;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpdateMaterialReservationRequestValidator : AbstractValidator<UpdateMaterialReservationRequest>
{
    public UpdateMaterialReservationRequestValidator()
    {
        RuleFor(x => x).Must(x => x.StartTime < x.EndTime)
            .WithMessage("StartTime must be before EndTime.");
    }
}
