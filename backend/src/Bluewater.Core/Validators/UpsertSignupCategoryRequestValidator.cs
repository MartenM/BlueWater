using Bluewater.Core.Dto.Signup;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertSignupCategoryRequestValidator : AbstractValidator<UpsertSignupCategoryRequest>
{
    public UpsertSignupCategoryRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
    }
}
