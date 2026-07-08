namespace Bluewater.Core.Dto.Availability;

public record AvailabilityBlockDto(Guid Id, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
