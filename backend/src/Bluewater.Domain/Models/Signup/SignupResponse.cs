using Bluewater.Domain.Auditing;
using Bluewater.Domain.Models;

namespace Bluewater.Domain.Models.Signup;

public class SignupResponse : IAuditable
{
    public Guid Id { get; set; }

    public Guid SignupId { get; set; }
    public Signup Signup { get; set; } = null!;

    public Guid UserId { get; set; }
    public BlueUser User { get; set; } = null!;

    public bool Reservation { get; set; }

    public ICollection<SignupResponseFieldValue> FieldValues { get; set; } = new List<SignupResponseFieldValue>();

    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
