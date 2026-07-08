using Bluewater.Core.Dto.Availability;
using FluentValidation;

namespace Bluewater.Core.Validators;

public class SetDayAvailabilityRequestValidator : AbstractValidator<SetDayAvailabilityRequest>
{
    public SetDayAvailabilityRequestValidator()
    {
        RuleForEach(x => x.Blocks).ChildRules(block =>
        {
            block.RuleFor(b => b.StartTime)
                .Must(BeOnFifteenMinuteBoundary)
                .WithMessage("Start time must be on a 15-minute boundary.");

            block.RuleFor(b => b.EndTime)
                .Must(BeOnFifteenMinuteBoundary)
                .WithMessage("End time must be on a 15-minute boundary.");

            block.RuleFor(b => b)
                .Must(b => b.StartTime < b.EndTime)
                .WithMessage("Start time must be before end time.")
                .WithName("StartTime");
        });

        RuleFor(x => x.Blocks)
            .Must(blocks => !HasOverlap(blocks))
            .WithMessage("Availability blocks must not overlap.");
    }

    private static bool BeOnFifteenMinuteBoundary(TimeOnly time) => time.Minute % 15 == 0 && time.Second == 0;

    private static bool HasOverlap(List<AvailabilityBlockInputDto> blocks)
    {
        var ordered = blocks.OrderBy(b => b.StartTime).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].StartTime < ordered[i - 1].EndTime)
                return true;
        }

        return false;
    }
}
