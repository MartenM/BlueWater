namespace Bluewater.Core.Dto.Availability;

public record MyWeekAvailabilityDto(DateOnly WeekStart, List<AvailabilityDayDto> Days);
