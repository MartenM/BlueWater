using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Groups;

public class UserGroupPermission : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;

    public BluePermission Permission { get; set; }

    public Guid? UserGroupCategoryRoleId { get; set; }
    public UserGroupCategoryRole? UserGroupCategoryRole { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
