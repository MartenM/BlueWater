using Bluewater.Domain.Models.Groups;

namespace Bluewater.Domain.Models.Clusters;

public class MemberClusterCriterion
{
    public Guid Id { get; set; }

    public Guid MemberClusterId { get; set; }
    public MemberCluster MemberCluster { get; set; } = null!;

    public ClusterCriterionType Type { get; set; }

    public Guid? UserGroupCategoryId { get; set; }
    public UserGroupCategory? UserGroupCategory { get; set; }

    public Guid? UserGroupCategoryRoleId { get; set; }
    public UserGroupCategoryRole? UserGroupCategoryRole { get; set; }

    public Guid? UserGroupId { get; set; }
    public UserGroup? UserGroup { get; set; }
}
