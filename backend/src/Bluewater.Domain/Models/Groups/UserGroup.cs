using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Groups;

public class UserGroup : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Administrative { get; set; }

    public Guid UserGroupCategoryId { get; set; }
    public UserGroupCategory UserGroupCategory { get; set; } = null!;

    public ICollection<UserGroupPermission> Permissions { get; set; } = new List<UserGroupPermission>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
