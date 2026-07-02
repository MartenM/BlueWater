using Bluewater.Core.Dto.Outings;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class SetParticipantRoleRequestValidator : AbstractValidator<SetParticipantRoleRequest>
{
    public SetParticipantRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
