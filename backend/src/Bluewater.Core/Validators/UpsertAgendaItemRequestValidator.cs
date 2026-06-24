using Bluewater.Core.Dto.Agenda;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class UpsertAgendaItemRequestValidator : AbstractValidator<UpsertAgendaItemRequest>
{
    public UpsertAgendaItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.EndDate)
            .Must((request, endDate) => endDate is null || endDate >= request.Date)
            .WithMessage("EndDate must be on or after Date.");

        RuleFor(x => x.EndTime)
            .Must((request, endTime) => endTime is null || request.Time is not null)
            .WithMessage("EndTime requires Time to also be set.")
            .Must((request, endTime) =>
            {
                if (endTime is null || request.Time is null)
                {
                    return true;
                }

                var start = request.Date.ToDateTime(request.Time.Value);
                var end = (request.EndDate ?? request.Date).ToDateTime(endTime.Value);
                return end > start;
            })
            .WithMessage("EndTime must be after the start of the agenda item.");
    }
}
