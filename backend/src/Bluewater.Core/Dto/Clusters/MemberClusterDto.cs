namespace Bluewater.Core.Dto.Clusters;

public record MemberClusterDto(
    Guid Id,
    string Name,
    string Description,
    List<MemberClusterCriterionDto> Criteria);
