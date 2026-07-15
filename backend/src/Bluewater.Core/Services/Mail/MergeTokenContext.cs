namespace Bluewater.Core.Services.Mail;

public record MergeTokenContext(IReadOnlyDictionary<string, string> Values)
{
    public static readonly MergeTokenContext Empty = new(new Dictionary<string, string>());

    public MergeTokenContext With(IReadOnlyDictionary<string, string> extra)
    {
        var merged = new Dictionary<string, string>(Values, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in extra)
            merged[key] = value;

        return new MergeTokenContext(merged);
    }
}
