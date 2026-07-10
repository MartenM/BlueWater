namespace Bluewater.Core.Dto.Availability;

public record OutingTimelineEntryDto(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    string? BoatName,
    string Label);
