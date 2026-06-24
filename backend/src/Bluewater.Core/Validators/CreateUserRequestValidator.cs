using Bluewater.Core.Dto.Users;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.UserName).NotEmpty()
            .MaximumLength(256)
            .MustAsync(async (userName, cancellationToken) =>
                !await db.Users.AnyAsync(x => x.UserName == userName, cancellationToken))
            .WithMessage(x => $"Username '{x.UserName}' is already in use.");

        RuleFor(x => x.Email).NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .MustAsync(async (email, cancellationToken) =>
                !await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            .WithMessage(x => $"Email '{x.Email}' is already in use.");

        RuleFor(x => x.Firstname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Surname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SurnamePrefix).MaximumLength(50);
        RuleFor(x => x.EmergencyPhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotEqual(default(DateOnly));
    }
}
