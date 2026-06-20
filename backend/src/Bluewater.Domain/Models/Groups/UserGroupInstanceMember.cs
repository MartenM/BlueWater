namespace Bluewater.Domain.Models.Groups;

public class UserGroupInstanceMember
{
    public Guid UserGroupInstanceId { get; set; }
    public UserGroupInstance UserGroupInstance { get; set; } = null!;

    public Guid UserId { get; set; }
    public BlueUser User { get; set; } = null!;
}
