namespace Bluewater.Core.Exceptions;

public class BlueValidationException : Exception
{
    public string Reason { get; init; }
    
    public BlueValidationException(string reason) : base("Validation failed. Reason: " + reason)
    {
        Reason = reason;
    }
}