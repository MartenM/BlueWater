using Bluewater.Core.Dto.Exams;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class UpsertExamTypeRequestValidator : AbstractValidator<UpsertExamTypeRequest>
{
    public UpsertExamTypeRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.Name).NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
