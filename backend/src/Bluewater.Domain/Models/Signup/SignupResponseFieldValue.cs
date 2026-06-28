namespace Bluewater.Domain.Models.Signup;

public class SignupResponseFieldValue
{
    public Guid Id { get; set; }

    public Guid ResponseId { get; set; }
    public SignupResponse Response { get; set; } = null!;

    public Guid FieldId { get; set; }
    public SignupInputField Field { get; set; } = null!;

    public string? Value { get; set; }
}
