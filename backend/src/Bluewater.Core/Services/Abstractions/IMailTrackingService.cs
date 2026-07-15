namespace Bluewater.Core.Services.Abstractions;

public interface IMailTrackingService
{
    /// <summary>Records a click and returns the original URL to redirect to, or null if the token is unknown.</summary>
    Task<string?> RecordClickAsync(string token);

    /// <summary>Records an open. No-ops silently if the token is unknown (public, unauthenticated endpoint).</summary>
    Task RecordOpenAsync(string token);
}
