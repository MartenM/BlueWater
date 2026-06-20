namespace Bluewater.Domain.Models.Groups;

public class UserGroupInstancePermission
{
    public Guid UserGroupInstanceId { get; set; }
    public UserGroupInstance UserGroupInstance { get; set; } = null!;

    public BluePermission Permission { get; set; }
}
