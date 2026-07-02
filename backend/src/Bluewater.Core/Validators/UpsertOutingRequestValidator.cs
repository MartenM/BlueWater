using Bluewater.Core.Dto.Outings;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class UpsertOutingRequestValidator : AbstractValidator<UpsertOutingRequest>
{
    public UpsertOutingRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.UserGroupInstanceId)
            .MustAsync(async (id, ct) => await db.UserGroupInstances.AnyAsync(x => x.Id == id, ct))
            .WithMessage(x => $"Group instance '{x.UserGroupInstanceId}' does not exist.");

        RuleFor(x => x.OutingDateEnd)
            .GreaterThan(x => x.OutingDate)
            .When(x => x.OutingDateEnd.HasValue)
            .WithMessage("OutingDateEnd must be after OutingDate.");

        RuleFor(x => x.BoatTypeDifferent)
            .Empty()
            .When(x => x.BoatTypeId.HasValue)
            .WithMessage("BoatTypeDifferent must be empty when BoatTypeId is set.");

        RuleFor(x => x.BoatTypeId)
            .MustAsync(async (id, ct) => id is null || await db.EquipmentTypes.AnyAsync(x => x.Id == id && x.IsBoat, ct))
            .WithMessage(x => $"BoatType '{x.BoatTypeId}' does not exist or is not a boat type.");

        RuleFor(x => x.BoatId)
            .MustAsync(async (id, ct) => id is null || await db.Equipment.AnyAsync(x => x.Id == id, ct))
            .WithMessage(x => $"Boat '{x.BoatId}' does not exist.");

        RuleFor(x => x)
            .MustAsync(async (request, ct) =>
            {
                if (request.BoatId is null || request.BoatTypeId is null)
                    return true;

                var boat = await db.Equipment.FirstOrDefaultAsync(x => x.Id == request.BoatId, ct);
                return boat is null || boat.EquipmentTypeId == request.BoatTypeId;
            })
            .WithMessage("Boat does not match the selected boat type.")
            .OverridePropertyName("BoatId");
    }
}
