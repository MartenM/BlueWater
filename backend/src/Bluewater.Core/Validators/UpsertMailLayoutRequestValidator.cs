using Bluewater.Core.Dto.Mail;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertMailLayoutRequestValidator : AbstractValidator<UpsertMailLayoutRequest>
{
    public UpsertMailLayoutRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.HeaderHtml).MaximumLength(20000);
        RuleFor(x => x.FooterHtml).MaximumLength(20000);
    }
}
