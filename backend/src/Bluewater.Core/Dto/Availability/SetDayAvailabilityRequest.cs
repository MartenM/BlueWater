namespace Bluewater.Core.Dto.Availability;

public record SetDayAvailabilityRequest(DateOnly Date, List<AvailabilityBlockInputDto> Blocks);
