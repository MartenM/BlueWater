namespace Bluewater.Infra.Options;

public class TokenOptions
{
    public string Secret { get; set; } = null!;
    public TimeSpan ExpireTime { get; set; }
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
}