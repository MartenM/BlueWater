using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Groups;

public class UserGroupInstance : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;

    public Guid SeasonId { get; set; }
    public BlueSeason Season { get; set; } = null!;

    public ICollection<UserGroupInstanceMember> Members { get; set; } = new List<UserGroupInstanceMember>();
    public ICollection<UserGroupInstancePermission> Permissions { get; set; } = new List<UserGroupInstancePermission>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
