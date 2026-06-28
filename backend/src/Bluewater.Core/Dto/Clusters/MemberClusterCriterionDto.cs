using Bluewater.Domain.Models.Clusters;

namespace Bluewater.Core.Dto.Clusters;

public record MemberClusterCriterionDto(
    Guid Id,
    ClusterCriterionType Type,
    Guid? CategoryId,
    string? CategoryName,
    Guid? RoleId,
    string? RoleName,
    Guid? GroupId,
    string? GroupName);
