using Bluewater.Domain.Models.Clusters;

namespace Bluewater.Core.Dto.Clusters;

public record AddClusterCriterionRequest(
    ClusterCriterionType Type,
    Guid? UserGroupCategoryId,
    Guid? UserGroupCategoryRoleId,
    Guid? UserGroupId);
