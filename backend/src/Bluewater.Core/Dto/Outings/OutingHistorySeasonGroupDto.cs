namespace Bluewater.Core.Dto.Outings;

public record OutingHistorySeasonGroupDto(
    Guid SeasonId,
    string SeasonName,
    IReadOnlyList<OutingMyInstanceDto> Instances);
