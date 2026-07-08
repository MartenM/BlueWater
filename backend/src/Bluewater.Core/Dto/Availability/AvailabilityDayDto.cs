namespace Bluewater.Core.Dto.Availability;

public record AvailabilityDayDto(DateOnly Date, List<AvailabilityBlockDto> Blocks);
