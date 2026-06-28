using Bluewater.Domain.Auditing;

namespace Bluewater.Domain.Models.Signup;

public class SignupCategory : IAuditable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Hidden { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
