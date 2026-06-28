using Bluewater.Core.Dto.Signup;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class SubmitResponseRequestValidator : AbstractValidator<SubmitResponseRequest>
{
    public SubmitResponseRequestValidator()
    {
        RuleForEach(x => x.FieldValues)
            .ChildRules(fv => fv.RuleFor(v => v.FieldId).NotEmpty());
    }
}
