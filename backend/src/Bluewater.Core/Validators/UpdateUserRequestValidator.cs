using Bluewater.Core.Dto.Users;
using Bluewater.Infra.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Bluewater.Core.Validators;

/// <summary>
/// Username/email uniqueness must exclude the user being updated. UserService passes the
/// target user's id via ValidationContext.RootContextData["UserId"] since the request DTO
/// itself doesn't carry it (the id comes from the route).
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator(BluewaterContext db)
    {
        RuleFor(x => x.UserName).NotEmpty()
            .MaximumLength(256)
            .MustAsync(async (request, userName, context, cancellationToken) =>
            {
                var currentId = (Guid)context.RootContextData["UserId"];
                return !await db.Users.AnyAsync(x => x.UserName == userName && x.Id != currentId, cancellationToken);
            })
            .WithMessage(x => $"Username '{x.UserName}' is already in use.");

        RuleFor(x => x.Email).NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .MustAsync(async (request, email, context, cancellationToken) =>
            {
                var currentId = (Guid)context.RootContextData["UserId"];
                return !await db.Users.AnyAsync(x => x.Email == email && x.Id != currentId, cancellationToken);
            })
            .WithMessage(x => $"Email '{x.Email}' is already in use.");

        RuleFor(x => x.Firstname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Surname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SurnamePrefix).MaximumLength(50);
        RuleFor(x => x.EmergencyPhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotEqual(default(DateOnly));
    }
}
