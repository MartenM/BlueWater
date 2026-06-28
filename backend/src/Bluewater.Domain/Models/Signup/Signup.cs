using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Signup;

public class Signup : IAuditable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    public SignupCategory? Category { get; set; }

    public DateTime? EndDate { get; set; }
    public bool AllowMultiple { get; set; }
    public bool AllowDelete { get; set; }
    public bool AllowUpdate { get; set; }
    public int? MaxSignups { get; set; }
    public int? MaxWaitlist { get; set; }
    public bool HideSignups { get; set; }
    public bool Anonymous { get; set; }

    public ICollection<SignupCluster> Clusters { get; set; } = new List<SignupCluster>();
    public ICollection<SignupInputField> InputFields { get; set; } = new List<SignupInputField>();
    public ICollection<SignupResponse> Responses { get; set; } = new List<SignupResponse>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
