using Bluewater.Domain.Models.Clusters;

namespace Bluewater.Domain.Models.Signup;

public class SignupCluster
{
    public Guid SignupId { get; set; }
    public Signup Signup { get; set; } = null!;

    public Guid MemberClusterId { get; set; }
    public MemberCluster MemberCluster { get; set; } = null!;
}
