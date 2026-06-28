namespace Bluewater.Domain.Models.Signup;

public class SignupInputField
{
    public Guid Id { get; set; }

    public Guid SignupId { get; set; }
    public Signup Signup { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public SignupInputFieldType Type { get; set; }
    public string? Options { get; set; }
    public bool Visible { get; set; }
    public int SortOrder { get; set; }

    public ICollection<SignupResponseFieldValue> FieldValues { get; set; } = new List<SignupResponseFieldValue>();
}
