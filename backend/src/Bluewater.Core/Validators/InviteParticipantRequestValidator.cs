using Bluewater.Core.Dto.Outings;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class InviteParticipantRequestValidator : AbstractValidator<InviteParticipantRequest>
{
    public InviteParticipantRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.UserId)
            .MustAsync(async (id, ct) => await db.Users.AnyAsync(x => x.Id == id, ct))
            .WithMessage(x => $"User '{x.UserId}' does not exist.");
    }
}
