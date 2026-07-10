using Bluewater.Core.Dto.MaterialPlanner;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class CreateMaterialReservationRequestValidator : AbstractValidator<CreateMaterialReservationRequest>
{
    public CreateMaterialReservationRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x).Must(x => x.StartTime < x.EndTime)
            .WithMessage("StartTime must be before EndTime.");

        RuleFor(x => x.EquipmentId)
            .MustAsync(async (equipmentId, cancellationToken) =>
                await db.Equipment.AnyAsync(e => e.Id == equipmentId && e.FreeFleet && !e.OutOfOrder, cancellationToken))
            .WithMessage(x => $"Equipment '{x.EquipmentId}' does not exist or is not available for reservation.");
    }
}
