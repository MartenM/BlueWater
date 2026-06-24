namespace Bluewater.Core.Dto.Seasons;

public record SeasonDto(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);
