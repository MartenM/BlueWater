using System.Text.RegularExpressions;

namespace Bluewater.Core.Services.Mail;

public partial class MergeTokenRenderer : IMergeTokenRenderer
{
    public string Render(string source, MergeTokenContext context)
    {
        return TokenPattern().Replace(source, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return context.Values.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}")]
    private static partial Regex TokenPattern();
}
