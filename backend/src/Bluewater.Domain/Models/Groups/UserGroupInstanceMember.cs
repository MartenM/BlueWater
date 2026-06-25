using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Groups;

public class UserGroupInstanceMember : IAuditableRelation
{
    public Guid UserGroupInstanceId { get; set; }
    public UserGroupInstance UserGroupInstance { get; set; } = null!;

    public Guid UserId { get; set; }
    public BlueUser User { get; set; } = null!;

    public Guid? UserGroupCategoryRoleId { get; set; }
    public UserGroupCategoryRole? UserGroupCategoryRole { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
